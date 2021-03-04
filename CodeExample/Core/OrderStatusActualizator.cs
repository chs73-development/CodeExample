using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ApsStatuses.DAL;
using ApsStatuses.Infrastructure;
using ApsStatuses.Service;

using APS.Domain;
using APS.Domain.Logging;
using APS.Domain.Entities;
using APS.Domain.Notification;
using APS.Domain.DB.Repositories;

namespace ApsStatuses
{
    /// <summary>
    /// Инкапсулирует методы, выполняющие актуализацию статусов заказов
    /// </summary>
    public class OrderStatusActualizator
    {
        /// <summary>
        /// Провайдер для взаимодействия с Rest-сервисом
        /// </summary>
        private UniversalStatusProvider statusProvider;

        /// <summary>
        /// Объект, содержащий методы для чтения/записи данных в БД.
        /// </summary>
        public OrderStatusDbRepository dbRepository;

        /// <summary>
        /// Передает настройки конфигурации работы со статусами
        /// </summary>
        private OrderStatusConfig config;

        /// <summary>
        /// Объект для хранения словаря маппинга соответствия статусов.
        /// </summary>
        private IDictionary<string, StatusIds> dictionary;

        
        /// <summary>
        /// Коллекция статусов, которые не удалось актуализировать. 
        /// </summary>
        private List<OrderOldStatusInfo> failedStatuses;
        
        /// <summary>
        /// Конструирует экземпляр класса на основании объекта, предоставляющего методы получения данных от Rest-сервиса, 
        /// объекта, предоставляющего методы для работы с БД, а также объекта конфигурации.
        /// </summary>
        /// <param name="statusProvider">Обьект, предоставляющий методы для выполнения запроса к Rest-сервису</param>
        /// <param name="dbRepository">Обьект, предоставляющий методы для работы с БД</param>
        /// <param name="config">Объект, предоставляющий данные конфигурации</param>
        /// <exception cref="ArgumentNullException">Генерируется, если любому из параметров передается значение null</exception>
        public OrderStatusActualizator(UniversalStatusProvider statusProvider, OrderStatusDbRepository dbRepository, OrderStatusConfig config)
        {
            this.dbRepository = dbRepository ?? throw new ArgumentNullException(nameof(dbRepository));
            this.statusProvider = statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));
            this.config = config ?? throw new ArgumentNullException(nameof(config));           
        }                

        /// <summary>
        /// Выполнить итерацию по актуализации статусов заказов.
        /// </summary>
        /// <param name="oldStatusesData">Коллекция, представляющая данные по заказам до актуализации статусов</param>
        /// <param name="isMainIteration">Это главная итерация? Если параметр == true, будет выполнено сохранение информации по заказам с неактуализированными статусами в БД</param>
        /// <returns>true, если удалось актуализировать статусы по всем заказам</returns>
        /// <exception cref="StatusDataAccessExсeption">Генерируется при неудачной попытке получить словарь маппинга статусов</exception>
        /// <exception cref="StatusApiServiceExсeption">Генерируется при неудачной попытке получения данных по статусам от Rest-сервиса</exception>
        /// <exception cref="ArgumentNullException">Генерируется при попытке передать параметру oldStatusesData значение null</exception>
        private async Task<bool> ASIterationExecuteAsync(List<OrderOldStatusInfo> oldStatusesData, bool isMainIteration = true)
        {
            if (oldStatusesData == null) throw new ArgumentNullException(nameof(oldStatusesData));

            if (this.dictionary == null) this.dictionary = await dbRepository.GetMapStatusesDictionaryAsync();
            OrdersStatusesActualInfo actualStatusInfo = await this.statusProvider.GetOrderStatusInfoAsync(oldStatusesData, dictionary);
            await this.dbRepository.SaveSuccessStatusDataAsync(actualStatusInfo.ActualSuccessStatusesData);

            if (actualStatusInfo.ActualFailedStatusesData == null) return false;
            else if (actualStatusInfo.ActualFailedStatusesData.Count() == 0) return false;
            else
            {
                this.failedStatuses = oldStatusesData.Where(old => actualStatusInfo.ActualFailedStatusesData.Select(a => a.OrderId).Contains(old.OrderId)).ToList();

                // Если выполняется первая итерация, сохранить данные по коллекции заказов, для которых не удалось обновить данные по статусам ни в текущей ни в предыдущей итерации.
                if (isMainIteration) await this.dbRepository.SaveFailedStatusDataAsync(actualStatusInfo.ActualFailedStatusesData
                    .Where(d => !oldStatusesData.Where(o => o.SuccessStatusRequest.HasValue).Where(o => o.SuccessStatusRequest.Value == false).Select(o => o.OrderId).Contains(d.OrderId)).ToList());
                return true;
            }
        }


        /// <summary>
        /// Запускает процесс актуализацию статусов заказов, выполняя итерации по актуализации статусов. Если после выполнения итерации актуализации статусов не будут получены все необходимые данные,
        /// процесс актуализации будет продолжен до тех пор пока либо не актуализируются статусы, либо пока не выполнится указанное в файле конфигурации количество дополнительных итераций.
        /// Этот метод необходимо выполнять периодически, с периодом, указанным в ТЗ.
        /// </summary>
        /// <param name="allowExtraIterations">Признак необходимости делать дополнительные итерации</param>
        /// <exception cref="StatusDataAccessExсeption">Генерируется при неудачных попытках получить данные из БД</exception>
        /// <exception cref="StatusApiServiceExсeption">Генерируется при неудачных попытках получить данные от Rest-сервиса</exception>            
        private async Task ASProcessExecuteAsync(bool allowExtraIterations)
        {
            List<OrderOldStatusInfo> oldStatusesData = await this.dbRepository.GetOldStatusDataAsync();
            if (oldStatusesData.Count() == 0)
            {
                this.dbRepository.Logger.SaveLogAsync(new LogMessage
                {
                    EventTime = DateTime.Now,
                    EventType = LogEventEventTypes.ProcessFailed.ToString(),
                    Level = LogEventLevels.Warning.ToString(),
                    Message = config.NoOrderMessage,
                });
                return;
            }
            if (await this.ASIterationExecuteAsync(oldStatusesData) && allowExtraIterations)
            {
                int period = this.config.UpdateStatusesPeriod / this.config.MultiplicityUpdateStatusesPeriod;
                for (int numberIteration = 1; numberIteration < config.MultiplicityUpdateStatusesPeriod; numberIteration++)
                {                 
                    await Task.Delay(period);
                    if (!await this.ASIterationExecuteAsync(this.failedStatuses, false)) return;
                }
            }
        }

        /// <summary>
        /// Выполняет актуализацию статусов заказов согласно ТЗ. Фактически является оберткой метода  ASProcessExecuteAsync с обработкой всех возможных исключительных ситуаций.         
        /// <param name="allowExtraIterations">Признак необходимости делать дополнительные итерации</param>
        /// </summary>        
        public async Task ASExecuteAsync(bool allowExtraIterations)
        {            
            try
            {
                await this.ASProcessExecuteAsync(allowExtraIterations);
            }
            catch (Service.StatusApiServiceExсeption exc)
            {
                string errorMess = $"{config.FatalRemoteServiceErrorMessage}: {exc.Message}";
                this.dbRepository.Logger.SaveLogAsync(exc, LogEventEventTypes.StatusRequestFailed, LogEventLevels.Fatal, errorMess);
            }
            catch (StatusDataAccessExсeption exc)
            {
                string errorMess = $"{config.FatalDbErrorMessage}: {exc.Message}";
                this.dbRepository.Logger.SaveLogAsync(exc, LogEventEventTypes.DataAccessError, LogEventLevels.Fatal, errorMess);
            }
            catch (Exception exc)
            {
                string errorMess = $"{config.FatalUnexpectedErrorMessage}: {exc.Message}";
                this.dbRepository.Logger.SaveLogAsync(exc, LogEventEventTypes.MainProcessFailed, LogEventLevels.Fatal, errorMess);
            }
        }
    }
}


