using System.Runtime.CompilerServices;
using Serilog;

namespace MyComicsManagerApi.Utils;

public static class LoggerExtensions
{
    // Source : 
    // https://stackoverflow.com/questions/29470863/serilog-output-enrich-all-messages-with-methodname-from-which-log-entry-was-ca
    // https://stackoverflow.com/questions/48268854/how-to-get-formatted-output-from-logevent/48272467#48272467
    public static ILogger Here(this ILogger logger,
        [CallerMemberName] string memberName = "--",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0) {
        return logger
            .ForContext("MemberName", memberName)
            .ForContext("FilePath", sourceFilePath)
            .ForContext("LineNumber", sourceLineNumber);
    }
}