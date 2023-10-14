using System.Reflection;
using Serilog;

namespace riolog {
	public static class Extensions {
		public static void CloseAndFlush(this Serilog.ILogger logger) {
			logger.Information("Flushing logger... =====LOG END");
			Serilog.Log.CloseAndFlush();
		}
		
		public static async ValueTask CloseAndFlushAsync(this Serilog.ILogger logger) {
			logger.Information("Flushing logger asynchronously...");
			await Serilog.Log.CloseAndFlushAsync();
			logger.Information("Logger flushed. =====LOG END");
		}
	}
	
	[Serializable, Flags]
	public enum LogTo {
		None    = 0,
		Console = 1,
		File    = 2,
		Debug   = 4,
		All     = Console | File | Debug
	}

	public class InternalLogFactory {
		/// <summary>
		///  Start a new logger with the specified logToBits and logPath.
		///  If logToBits is LogTo.None, this method does nothing.
		///  If logPath is null or whitespace, the default log path is used.
		///  If logToBits is LogTo.All, all logging options are enabled.
		/// </summary>
		/// <param name="logToBits">Flags for what logging services to use</param>
		/// <param name="logPath">Path to directory where logs should be stored</param>
		public static ILogger SetupAndStart(LogTo logToBits, string? logPath = default) {
			if (logToBits == LogTo.None)
				return null!;

			if (string.IsNullOrWhiteSpace(logPath))
				logPath = LogPath.Get();

			var logConfig = new LoggerConfiguration().MinimumLevel.Debug();

			if (logToBits == LogTo.All) {
				logConfig
				   .WriteTo.Console()
				   .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
				   .WriteTo.Debug();
			}
			else {
				if ((logToBits & LogTo.Console) != 0)
					logConfig.WriteTo.Console();
				if ((logToBits & LogTo.File) != 0)
					logConfig.WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
				if ((logToBits & LogTo.Debug) != 0)
					logConfig.WriteTo.Debug();
			}	
			
			var          logger   = Log.Logger = logConfig.CreateLogger();

			const string startMsg = "------ Logging started ----- ";
			var          time     = DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss");
			
			logger.Information($"{startMsg}" + "{Time}", time);
			
			return logger;
		}

		static class LogPath {
			public static string Get() {
				var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
				path += @"\logging\log_.txt";
				return path;
			}
		}
	}
}