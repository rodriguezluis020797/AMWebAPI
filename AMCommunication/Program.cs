using AMData.Models.CoreModels;
using AMTools.Tools;
using AMWebAPI.Services.DataServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AMCommunication
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var logger = new AMDevLogger();
            logger.LogInfo("+");

            var config = new ConfigurationManager();
            config.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var optionsBuilder = new DbContextOptionsBuilder<AMCoreData>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("CoreConnectionString"));
            DbContextOptions<AMCoreData> options = optionsBuilder.Options;

            try
            {
                var comms = new List<UserCommunicationModel>();
                using (var _coreData = new AMCoreData(options, config))
                {
                    comms = _coreData.UserCommunications
                        .Where(x => x.DeleteDate == null)
                        .ToList();

                    foreach (var comm in comms)
                    {
                        Console.WriteLine(comm.Message);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
            }
            logger.LogInfo("-");
            Console.ReadKey();
        }
    }
}
