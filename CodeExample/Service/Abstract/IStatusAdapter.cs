using System.Threading.Tasks;
using System.Collections.Generic;
using ApsStatuses.DAL;

using APS.Domain;
using APS.Domain.Entities;

namespace ApsStatuses.Service
{    
    /// <summary>
    /// Содержит метод для получения актуальной информации по статусам заказов от веб-сервисов служб доставки
    /// </summary>
    public interface IStatusAdapter
    {
        /// <summary>
        /// Получить актуальные данные по статусам.
        /// </summary>
        /// <param name="orderStatusCollection">Коллекция объектов, содержащих данные по статусам заказов из БД</param>
        /// <param name="dictionary">Объект, представляющий словарь соответствия внутренних статусов и статусов СД</param>
        /// <returns>Возвращает объект ActionDataResult, инкапсулирующий актуальные данные по статусам заказов</returns>
        /// <exception cref="StatusApiServiceExсeption">Специальное исключение для проекта по статусам для ошибок при получении и обработке данных от Rest-сервиса</exception>
        /// <exception cref="ArgumentNullException">Генерируется, если входные параметры имеют значение null</exception>
        Task<OrdersStatusesActualInfo> GetOrderStatusInfoAsync(List<OrderStatusInfo> orderStatusCollection, IDictionary<string, StatusIds> dictionary);
    }
}
