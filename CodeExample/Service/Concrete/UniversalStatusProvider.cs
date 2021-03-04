using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using APS.Domain;
using APS.Domain.Entities;

namespace ApsStatuses.Service
{
    /// <summary>
    /// Реализует метод получения данных о статусах заказов посредством предоставленного объекта адаптрера    
    /// </summary>
    public class UniversalStatusProvider
    {
        /// <summary>
        /// Объект, содержащий метод для получения актуальных статусов заказов от интегратора Доставим
        /// </summary>
        private readonly IStatusAdapter statusAdapter;

        /// <summary>
        /// Конструктор класса UniversalStatusProvider.
        /// </summary>
        /// <param name="statusAdapter">Объект, содержащий метод для получения актуальных статусов заказов с помощью Rest-сервиса API Ship</param>
        /// <param name="dictionary">Объект, представляющий словарь маппинга соответствия статусов</param>
        /// <exception cref="ArgumentNullException">Генерируется, если параметр apiShipStatusAdapter имеет значение null</exception>
        public UniversalStatusProvider(IStatusAdapter statusAdapter)
        {
            this.statusAdapter = statusAdapter;        
        }
        /// <summary>
        /// Метод для получения объекта, инкапсулирующего данные после актуализации статусов заказов.
        /// </summary>
        /// <param name="orderStatusCollection">Коллекция объектов с данными о статусах заказов, актуализированных в предыдущей итерации</param>
        /// <returns>Объект, представляющий информацию об успешности получения данных и актуальные данные о статусах заказов</returns>
        /// <exception cref="ArgumentNullException">Генерируется, если любой из параметров имеет значение null</exception>
        /// <exception cref="StatusApiServiceExсeption">Генерируется методом apiShipStatusAdapter.GetApiShipOrderStatusInfoAsync</exception>
        public Task<OrdersStatusesActualInfo> GetOrderStatusInfoAsync(List<OrderStatusInfo> orderStatusCollection, IDictionary<string, StatusIds> dictionary)
        {
            if (orderStatusCollection == null) throw new ArgumentNullException(nameof(orderStatusCollection));
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
            return this.statusAdapter.GetOrderStatusInfoAsync(orderStatusCollection, dictionary);
        }
    }
}
