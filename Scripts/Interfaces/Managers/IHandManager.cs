using Maximagus.Scripts.Enums;
using Scripts.Commands.Hand;

public interface IHandManager
{
    AddCardCommand GetDrawCardCommand();

    int GetCardsToDraw();
}