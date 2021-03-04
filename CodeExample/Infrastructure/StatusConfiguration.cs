using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;

/// <summary>
/// Представляет дополнительные сущности, необходимые для огранизации процесса актуализации статусов заказов.
/// </summary>
namespace ApsStatuses.Infrastructure
{
    /// <summary>
    /// Предоставляет объект конфигурации IConfiguration на основании json файла.
    /// </summary>
    /// TODO: Убрать костыли определения пути к файлу конфигурации
    public class StatusConfiguration
    {
        /// <summary>
        /// Предоставляет объект конфигурации.
        /// </summary>
        public IConfigurationRoot AppConfiguration { get; set; }
        /// <summary>
        /// Конструктор класса.
        /// </summary>
        public StatusConfiguration()
        {  
            var builder = new ConfigurationBuilder()
                .AddJsonFile("statusconfig.json", optional: true, reloadOnChange: true)
                .AddJsonFile("delLinSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("pecomSettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("boxberrySettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("pickPointSettings.json", optional: true, reloadOnChange: true)
                ;
            AppConfiguration = builder.Build();
        }
    }
}