using System.Collections.Generic;
using System.Threading.Tasks;

using APS.Domain.DB;
using APS.Domain.Lib;
using APS.Domain.Logging;

/// <summary>
/// Содержит средства доступа к данным БД
/// </summary>
namespace ApsStatuses.DAL
{
    /// <summary>
    /// Содержит методы, позволяющие получать и изменять данные в БД, 
    /// используя внутреннее соглашение об именовании классов и хранимых процедур.
    /// </summary>
    public class StatusDbProcNameAdaptation: DbRepositoryBase
    {
        protected readonly ApsDbConfig _config;

        /// <summary>
        /// Конструирует объект класса на основании объекта конфигурации и объекта, предоставляющего методы записи лога в файл.
        /// </summary>
        /// <param name="config">Объект, предоставляющий данные конфигурации для работы с БД</param>
        /// <param name="writer">Объект, предоставляющий методы для записи лога в файл</param>
        public StatusDbProcNameAdaptation(ApsDbConfig config, LogFileWriter writer) : base(config.ConnectionString, writer)
        {
            this._config = config;
        }

        /// <summary>
        /// Прочитать данные, возвращаемые хранимой процедурой.
        /// Имя хранимой процедуры определяется по имени класса.
        /// </summary>
        /// <typeparam name="T">Класс со свойствами, имена и типы которых соответствуют читаемым из х.п. данным</typeparam>
        /// <returns>Коллекция объектов класса Т</returns>
        /// <exception cref="StatusDataAccessExсeption">Генерируется в результате обработки любых внутренних исключений</exception> 
        protected async Task<List<T>> ReadDataAsync<T>() where T : new()
        {            
            string procName = this._config.PrefixForReadProc + typeof(T).Name;
            return await base.ReadDataFromSPAsync<T>(base.GetDataFromReaderAsync<T>, procName);
        }

        /// <summary>
        /// Обновить данные в таблице БД с помощью хранимой процедуры.
        /// Имя хранимой процедуры определяется по имени класса.
        /// </summary>
        /// <typeparam name="T">Класс со свойствами, имена и типы которых соответствуют параметрам х.п.</typeparam>
        /// <param name="obj">Объект класса Т</param>        
        /// <exception cref="StatusDataAccessExсeption">Генерируется в результате обработки любых внутренних исключений</exception> 
        protected async Task UpdateDataAsync<T>(List<T> list) where T : new()
        {
            string procName = this._config.PrefixForUpdateProc + typeof(T).Name;
            await base.UpsertDataAsync(list, procName);
        }        
    }
}
