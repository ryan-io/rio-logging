using System.Reflection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Serilog.ILogger;

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

		public static Microsoft.Extensions.Logging.ILogger AsLogger<T>(this ILogger logger) {
			ILoggerFactory factory = new LoggerFactory().AddSerilog(Log.Logger);
			return factory.CreateLogger<T>();
		}

		public static Microsoft.Extensions.Logging.ILogger AsLogger(this ILogger logger) {
			ILoggerFactory factory = new LoggerFactory().AddSerilog(Log.Logger);
			return factory.CreateLogger<ILoggerFactory>();
		}

		public static void CloseAndFlush(this Microsoft.Extensions.Logging.ILogger logger) {
			Log.Logger.Information("Flushing logger... =====LOG END");
			Serilog.Log.CloseAndFlush();
		}

		public static async ValueTask CloseAndFlushAsync(this Microsoft.Extensions.Logging.ILogger logger) {
			Log.Logger.Information("Flushing logger asynchronously...");
			await Serilog.Log.CloseAndFlushAsync();
			Log.Logger.Information("Logger flushed. =====LOG END");
		}
	}

	[Serializable, Flags]
	public enum Output {
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
		/// <param name="outputBits">Flags for what logging services to use</param>
		/// <param name="logPath">Path to directory where logs should be stored</param>
		public static ILogger SetupAndStart(Output outputBits, string? logPath = default) {
			if (outputBits == Output.None)
				return null!;

			if (string.IsNullOrWhiteSpace(logPath))
				logPath = LogPath.Get();

			var logConfig = new LoggerConfiguration().MinimumLevel.Debug();

			if (outputBits == Output.All) {
				logConfig
				   .WriteTo.Console()
				   .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
				   .WriteTo.Debug();
			}
			else {
				if ((outputBits & Output.Console) != 0)
					logConfig.WriteTo.Console();
				if ((outputBits & Output.File) != 0)
					logConfig.WriteTo.File(logPath, rollingInterval: RollingInterval.Day);
				if ((outputBits & Output.Debug) != 0)
					logConfig.WriteTo.Debug();
			}

			var logger = Log.Logger = logConfig.CreateLogger();

			const string startMsg = "------ Logging started ----- ";
			var          time     = DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss");

			logger.Information($"{startMsg}" + "{Time}", time);

			return logger;
		}

		public static Microsoft.Extensions.Logging.ILogger SetupAndStartAsLogger(Output outputBits,
			string? logPath = default) => SetupAndStart(outputBits, logPath).AsLogger();

		static class LogPath {
			public static string Get() {
				var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
				path += @"\logging\log_.txt";
				return path;
			}
		}
	}
}