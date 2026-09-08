using AutoMapper;
using projectManager2Core.DTOs;
using projectManager2Core.Models;

namespace projectManager2Service.Mapping;

/// <summary>
/// הגדרת מיפויי AutoMapper בין Models (Entities) ל-DTOs.
///
/// שדות שדורשים מידע נוסף שלא בהכרח זמין על ה-Entity ברגע המיפוי (כמו
/// UserName/UserEmail על EventMemberDto, או AssignedToUserName על TaskDto -
/// שניהם מגיעים מ-User שנטען בנפרד ב-Service, לא מ-Navigation Property
/// טעונה תמיד) מוגדרים כאן Ignore, ומוזנים ידנית ב-Service מיד אחרי הקריאה
/// ל-_mapper.Map. כך נמנעים מהסתמכות על Include-ים נסתרים בפרופיל המיפוי,
/// ושומרים בדיוק על אותה התנהגות שהייתה קודם עם המיפוי הידני.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();

        CreateMap<Event, EventDto>();

        CreateMap<EventMember, EventMemberDto>()
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.UserEmail, opt => opt.Ignore());

        CreateMap<TaskEntity, TaskDto>()
            .ForMember(dest => dest.AssignedToUserName, opt => opt.Ignore());
    }
}
