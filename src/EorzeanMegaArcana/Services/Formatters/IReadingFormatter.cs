using EorzeanMegaArcana.Models;

namespace EorzeanMegaArcana.Services.Formatters;

public interface IReadingFormatter
{
    string Format(ReadingResult result, Doctrine doctrine);
}
