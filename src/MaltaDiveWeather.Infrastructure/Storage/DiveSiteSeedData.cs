using MaltaDiveWeather.Domain.Entities;

namespace MaltaDiveWeather.Infrastructure.Storage;

internal static class DiveSiteSeedData
{
    public static IReadOnlyList<DiveSite> Create()
    {
        return
        [
            new DiveSite(
                1,
                "Cirkewwa",
                "Malta",
                35.9979D,
                14.3297D,
                382D,
                226D),
            new DiveSite(
                2,
                "Ghar Lapsi",
                "Malta",
                35.8275D,
                14.4447D,
                602D,
                532D),
            new DiveSite(
                3,
                "Zurrieq Blue Grotto",
                "Malta",
                35.8194D,
                14.4576D,
                688D,
                542D),
            new DiveSite(
                4,
                "Exiles Sliema",
                "Malta",
                35.9138D,
                14.5068D,
                768D,
                365D),
            new DiveSite(
                5,
                "Dwejra Blue Hole",
                "Gozo",
                36.0489D,
                14.1889D,
                101D,
                120D),
        ];
    }
}
