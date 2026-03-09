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
            new DiveSite(
                6,
                "Patrol Boat P33",
                "Malta",
                35.8658D,
                14.5755D,
                905D,
                458D),
            new DiveSite(
                7,
                "Fra Ben Cave",
                "Malta",
                35.9601D,
                14.4277D,
                596D,
                288D),
            new DiveSite(
                8,
                "Cathedral Cave",
                "Gozo",
                36.0811D,
                14.2300D,
                178D,
                68D),
            new DiveSite(
                9,
                "Slugs Bay",
                "Malta",
                35.9797D,
                14.3612D,
                451D,
                256D),
            new DiveSite(
                10,
                "Anchor",
                "Malta",
                35.9603D,
                14.3397D,
                404D,
                288D),
            new DiveSite(
                11,
                "P31 Wreck",
                "Comino",
                36.0090D,
                14.3231D,
                375D,
                199D),
            new DiveSite(
                12,
                "Xlendi Reef",
                "Gozo",
                36.0288D,
                14.2133D,
                144D,
                163D),
            new DiveSite(
                13,
                "X127",
                "Malta",
                35.9009D,
                14.5038D,
                755D,
                395D),
        ];
    }
}
