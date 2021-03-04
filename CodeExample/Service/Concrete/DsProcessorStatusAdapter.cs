using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using APS.Domain;
using APS.Domain.Entities;
using APS.Domain.Logging;
using APS.Domain.Statuses;

using APS.Integrations.DsProcessor.Interfaces;
using APS.Integrations.DsProcessor.Results;

using ApsStatuses.Service;
using ApsStatuses.Infrastructure;

namespace APS.Statuses.Service.Concrete
{
    /// <summary>
    /// Инкапсулирует метод получения статусов заказов от объекта, представляющего логику интегратора Доставим
    /// </summary>
    internal class DsProcessorStatusAdapter : IStatusAdapter
    {
        /// <summary>
        /// Интегратор служб доставки сервиса Доставим
        /// </summary>
        private readonly IDsProcessor _dsProcessor;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger _logger;

        private readonly OrderStatusConfig config;

        /// <summary>
        /// Конструирует экземпляр по объекту интегратора Доставим и объекту логгера
        /// </summary>
        /// <param name="dsProcessor">Объект интегратора Доставим</param>
        /// <param name="logger">Логгер</param>
        public DsProcessorStatusAdapter(IDsProcessor dsProcessor
            , ILogger logger
            , OrderStatusConfig config
            //
            )
        {
            this._dsProcessor = dsProcessor;
            this._logger = logger;
            this.config = config;
        }

        /// <summary>
        /// Создать ключ словаря
        /// </summary>
        /// <param name="orderStatusResult">Объект, представляющий информацию о статусе заказа</param>
        /// <param name="dsKey">Идентификатор службы доставки</param>
        /// <returns></returns>
        private static String CreateDictKey(DsProviderOrderStatusResult orderStatusResult, String dsKey)
        {
            var dictKey = $"{orderStatusResult.StatusCode}{orderStatusResult.StatusNameForDictionary}{dsKey.ToLower()}";
            return dictKey;
        }

        /// <summary>
        /// Получить актуальную информацию о статусах заказов
        /// </summary>
        /// <param name="orderStatusCollection">Данные по статусам ЕЩЁ ПОКА не доставленных заказов.</param>
        /// <param name="dictionary">Словарь с ключами для сопоставления статусов</param>
        /// <exception cref="ArgumentNullException">Генерируется, если входные параметры имеют значение null</exception> 
        /// <exception cref="StatusApiServiceExсeption">Генерируется методом GetOrderStatusesInfoAsync</exception>
        public async Task<OrdersStatusesActualInfo> GetOrderStatusInfoAsync(List<OrderStatusInfo> orderStatusCollection
            , IDictionary<String, StatusIds> dictionary
            //
            )
        {
            if (orderStatusCollection == null)
            {
                throw new ArgumentNullException(nameof(orderStatusCollection));
            }

            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }
            OrderStatusInfo[] collFiltered = orderStatusCollection
                .Where(x => this._dsProcessor.SupportedByProcessorDsKeys.Any(y => String.Equals(x.DeliveryServiceId, y, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            OrderStatusInfo[] failedListAsExcluded = orderStatusCollection.Except(collFiltered).ToArray();

            var successList = new List<OrderStatusActualSuccessInfo>();

            var failedList = new List<OrderStatusActualFailedInfo>(failedListAsExcluded
                .Select(info => new OrderStatusActualFailedInfo
                {
                    OrderId = info.OrderId,
                    FailedDateTime = DateTime.Now,
                }));

            Dictionary<string, DsProviderOrderStatusResult[]> allStatuses = await this._dsProcessor.GetOrderStatusesInfoAsync(collFiltered, false);
            if (allStatuses == null)
                allStatuses = new Dictionary<string, DsProviderOrderStatusResult[]>();

            SortedSet<int> receivedOrderIds = new SortedSet<int>();
            if (allStatuses != null)
                receivedOrderIds = new SortedSet<Int32>(allStatuses.SelectMany(s => s.Value).Select(sr => sr.OrderId));
            // Список заказов, на которые не получилось получить статус.
            foreach (OrderStatusInfo statusInfo in collFiltered.Where(x => !receivedOrderIds.Contains(x.OrderId)))
            {
                failedList.Add(new OrderStatusActualFailedInfo
                {
                    OrderId = statusInfo.OrderId,
                    FailedDateTime = DateTime.Now,
                });

                this._logger.SaveLogAsync(new LogMessage
                {
                    OrderId = statusInfo.OrderId,
                    Message = $"{config.OrderStatusIsFail} {statusInfo.DeliveryServiceId}: {statusInfo.OrderId}",
                    EventType = LogEventEventTypes.StatusRequestFailed.ToString(),
                    Level = LogEventLevels.Error.ToString(),
                    EventTime = DateTime.Now

                });
            }

            foreach (KeyValuePair<String, DsProviderOrderStatusResult[]> keyValuePair in allStatuses)
            {
                // statuses - только успешные статусы. Что-то пришло, а что - пока не ясно т.к. может статус новый какой-то быть.
                // Проверится ниже.
                IReadOnlyList<DsProviderOrderStatusResult> statuses = keyValuePair.Value;
                var dsKey = keyValuePair.Key;

                // Проверка на наличие пришедшего статуса и сопоставления ему в словаре статусов.
                foreach (DsProviderOrderStatusResult orderInfo in statuses)
                {
                    var dictKey = CreateDictKey(orderInfo, dsKey);

                    if (dictionary.TryGetValue(dictKey.ToLowerInvariant(), out StatusIds statusIds))
                    {
                        successList.Add(new OrderStatusActualSuccessInfo
                        {
                            OrderId = orderInfo.OrderId,
                            DeliveryServiceStatusId = statusIds.DeliveryServiceStatusId,
                            StatusId = statusIds.StatusId,
                            // Может быть не заполнено, если параллельно кто запускал получение статусов в интеграции. Ну или просто забыл.
                            LastStatusSyncTime = orderInfo.LastStatusSyncTime ?? DateTime.Now,
                            DeliveryServiceOrderId = orderInfo.DeliveryServiceOrderId,
                            CityName = orderInfo.CityName,
                            ReturnOrderNumber = orderInfo.ReturnOrderNumber,

                        });
                    }
                    else
                    {
                        failedList.Add(new OrderStatusActualFailedInfo
                        {
                            OrderId = orderInfo.OrderId,
                            // Может быть не заполнено, если параллельно кто запускал получение статусов в интеграции. Ну или просто забыл.
                            FailedDateTime = orderInfo.LastStatusSyncTime ?? DateTime.Now,
                        });

                        this._logger.SaveLogAsync(new LogMessage
                        {
                            OrderId = orderInfo.OrderId,                           
                            Message = $"{config.DictKeyIsUnexpectedErrorNess} {dsKey}: {dictKey}",
                            EventType = LogEventEventTypes.GetMapStatusesDictionaryFailed.ToString(),
                            Level = LogEventLevels.Error.ToString(),
                            EventTime = DateTime.Now

                        });
                    }
                }
            }

            return new OrdersStatusesActualInfo
            {
                ActualSuccessStatusesData = successList,
                ActualFailedStatusesData = failedList,
            };
        }
    }
}
