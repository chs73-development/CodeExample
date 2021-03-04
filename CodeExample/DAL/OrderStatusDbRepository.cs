using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using APS.Domain;
using APS.Domain.Lib;
using APS.Domain.Logging;
using APS.Domain.Entities;

namespace ApsStatuses.DAL
{
    /// <summary>
    /// Содержит методы работы с данными, необходимые для реализации задачи по актуализации статусов заказов. Р
    /// Реализует интерфейс IOrderStatusDbRepository.
    /// </summary>
    /// TODO: Доработать механизм оповещения по статусам заказов в случае регулярно повторяющейся ошибки записи в БД.
    public class OrderStatusDbRepository : StatusDbProcNameAdaptation
    {
        /// <summary>
        /// Конструирует объект класса на основании объекта конфигурации и объекта, предоставляющего методы записи лога в файл.
        /// </summary>
        /// <param name="config">Объект, предоставляющий данные конфигурации для работы с БД</param>
        /// <param name="writer">Объект, предоставляющий методы для записи лога в файл</param>
        public OrderStatusDbRepository(ApsDbConfig config, LogFileWriter writer) : base(config, writer) { }        

        /// <summary>
        /// Получить словарь маппинга соответствия статусов, где ключ - сумма строк имени, кода статусов СД и идентификатора СД, 
        /// а значение - объект, содержащий идентификатор внутреннего статуса и идентификатор внешнего статуса.
        /// </summary>
        /// <returns>словарь маппинга соответствия статусов, где ключ - сумма строк имени, кода статусов СД и идентификатора СД, 
        /// а значение - объект, содержащий идентификатор внутреннего статуса и идентификатор внешнего статуса.</returns>
        /// <exception cref="StatusDataAccessExсeption">Специальное исключение для проекта по статусам для ошибок операций с БД</exception>     
        public async Task<IDictionary<string, StatusIds>> GetMapStatusesDictionaryAsync()
        {            
            IEnumerable<MapStatuses> data;
            data = await ReadDataAsync<MapStatuses>();
            Dictionary<string, StatusIds> dictionary = new Dictionary<string, StatusIds>();
            foreach (MapStatuses map in data)
            {
                string dictKey = $"{map.DeliveryServiceStatusCode}{map.DeliveryServiceStatusName}{map.DeliveryServiceId}";
                try
                {
                    dictionary.Add(dictKey, new StatusIds { DeliveryServiceStatusId = map.Id, StatusId = map.StatusId });
                }
                catch (ArgumentException exc)
                {
                    string errorMessage = $"{_config.GetStatusDictErrorMessage}: {dictKey}";
                    Logger.SaveLogAsync(exc, LogEventEventTypes.GetMapStatusesDictionaryFailed, LogEventLevels.Error, errorMessage);
                    throw new StatusDataAccessExсeption(errorMessage, exc);
                }
            }
            if (dictionary.Count == 0)
            {
                string errorMess = _config.SatusDictIsEmptyErrorMessage;
                StatusDataAccessExсeption exc = new StatusDataAccessExсeption(errorMess);
                base.Logger.SaveLogAsync(exc, LogEventEventTypes.GetMapStatusesDictionaryFailed, LogEventLevels.Error, errorMess);
                throw exc;
            }
            return dictionary;
        }

        /// <summary>
        /// Сохранить данные, содержащиесь в коллекции объектов класса T в базе данных.
        /// </summary>
        /// <typeparam name="T">Объект модели данных</typeparam>
        /// <param name="list">Список объектов, представляющая сохраняемые данные</param>        
        /// <exception cref="StatusDataAccessExсeption">Генерируется в результате обработки любых внутренних исключений</exception> 
        private async Task SaveDataAsync<T>(List<T> list) where T : IOrder, new()
        {
            await this.UpdateDataAsync<T>(list);
        }

        /// <summary>
        /// Получить коллекцию объектов, содержащих данные по статусам всех актуальных заказов.
        /// </summary>
        /// <returns>Объект, содержащий информацию об успешности завершения операции</returns>
        /// <exception cref="StatusDataAccessExсeption">Генерируется при обработке любых внутренних исключений</exception> 
        public async Task<List<OrderOldStatusInfo>> GetOldStatusDataAsync()
        {
            return await ReadDataAsync<OrderOldStatusInfo>();
        }

        /// <summary>
        /// Сохраняет данные о заказах с неудачно завершенными запросами обновления статусов. 
        /// Выполняется ассинхронно в потоке из пула потоков, не задерживая выполнение вызывавающего потока.
        /// </summary>        
        /// <param name="actualFailedStatusData">Коллекция с данными о заказах с неудачно завершенными запросами обновления статусов</param>
        /// <exception cref="StatusDataAccessExсeption">Генерируется в результате обработки любых внутренних исключений</exception>           
        public async Task SaveFailedStatusDataAsync(List<OrderStatusActualFailedInfo> actualFailedStatusData)
        {
            await SaveDataAsync(actualFailedStatusData);
        }

        /// <summary>
        /// Сохраняет данные о заказах после удачно завершенных запросов обновления статусов.
        /// Выполняется ассинхронно в потоке из пула потоков, не задерживая выполнение вызывавающего потока.
        /// </summary>
        /// <param name="actualSuccessStatusData">Коллекция с данными о заказах с актуализированными статусами</param>        
        public async Task SaveSuccessStatusDataAsync(List<OrderStatusActualSuccessInfo> actualSuccessStatusData)
        {
            await SaveDataAsync(actualSuccessStatusData);
        }
        
        ///<summary>
        /// Получить данные из журнала важных событий
        /// </summary>  
        /// <returns>Коллекция объектов, содержащая данные журнала важных событий</returns>
        /// <exception cref="StatusDataAccessExeption">Специальное исключение для проекта по статусам для ошибок операций с БД</exception> 
        public async Task<List<EventLogOrderInfo>> GetEventLogOrderInfoAsync()
        {            
            return await  ReadDataAsync<EventLogOrderInfo>();            
        }
        /// <summary>
        /// Получить данные об идентификаторах и именах СД.
        /// </summary>   
        /// <exception cref="StatusDataAccessExсeption">Специальное исключение для проекта по статусам для ошибок операций с БД</exception> 
        /// <returns>Коллекция данных с идентификаторами и именами СД.</returns>
        public async Task<List<DeliveryServiceIdAndName>> GetDsKeyNameAsync()
        {
            return await ReadDataAsync<DeliveryServiceIdAndName>();
        }
               
    }
}
