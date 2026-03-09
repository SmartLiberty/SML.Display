namespace SML.Display.Core.MappingProfiles;

using AutoMapper;
using Google.Protobuf.WellKnownTypes;

public class TimeMappingProfile : Profile
{
    public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:sszzz"; // Include timezone offset

    public TimeMappingProfile()
    {
        CreateMap<Timestamp, string>()
            .ConvertUsing(s => FromTimestampToString(s));

        CreateMap<DateTimeOffset, string>()
            .ConvertUsing(s => s.ToString(DateTimeFormat));

        CreateMap<DateTimeOffset, Timestamp>()
            .ConvertUsing(s => s.UtcDateTime.ToTimestamp()); // Convert DateTimeOffset to UTC DateTime before to Timestamp

        CreateMap<Timestamp, DateTimeOffset>()
            .ConvertUsing(s => TimestampToDateTimeOffset(s)); // Correct conversion using a helper method

        CreateMap<DateTime, DateTimeOffset>()
            .ConvertUsing(s => new DateTimeOffset(DateTime.SpecifyKind(s, DateTimeKind.Utc))); // Convert DateTime to DateTimeOffset specifying as UTC

        CreateMap<DateTimeOffset, DateTime>()
            .ConvertUsing(s => s.DateTime); // Return the DateTime part, assuming you know the context
    }

    private static string FromTimestampToString(Timestamp time)
        => TimestampToDateTimeOffset(time).ToString(DateTimeFormat);

    private static DateTimeOffset TimestampToDateTimeOffset(Timestamp time)
    {
        if (time == null)
        {
            // Handle the null case, e.g. by returning a default value
            return new(DateTime.UnixEpoch);
        }
        return new DateTimeOffset(DateTime.SpecifyKind(time.ToDateTime(), DateTimeKind.Utc));
    }
}

