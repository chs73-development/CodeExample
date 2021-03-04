
namespace ApsStatuses.DAL
{
    /// <summary>
    /// Предоставляет данные об идентификаторе и имени СД.
    /// Имя класса соответствует имени х.п. GetDsKeyName.
    /// Имена свойств соответствуют названиям столбцов, возвращаемых х.п. GetDeliveryServiceIdAndName.
    /// </summary>
    public class DeliveryServiceIdAndName
    {
        /// <summary>
        /// Идентификатор СД.
        /// </summary>        
        public string DeliveryServiceId { get; set; }
        /// <summary>
        /// Имя СД.
        /// </summary>
        public string DeliveryServicesName { get; set; }
    }
}
