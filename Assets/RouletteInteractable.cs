using UnityEngine;

public class RouletteInteractable : MonoBehaviour
{
    public enum InteractableType
    {
        Spin,
        Red,
        Black,
        Green,
        BetUp,
        BetDown
    }

    public InteractableType type = InteractableType.Spin;

    [Header("Roulette Machine Reference")]
    public RouletteMachine rouletteMachine;

    public void Interact()
    {
        if (rouletteMachine == null) return;

        switch (type)
        {
            case InteractableType.Spin:
                rouletteMachine.Spin();
                break;

            case InteractableType.Red:
                rouletteMachine.SetBetTypeRed();
                break;

            case InteractableType.Black:
                rouletteMachine.SetBetTypeBlack();
                break;

            case InteractableType.Green:
                rouletteMachine.SetBetTypeGreen();
                break;

            case InteractableType.BetUp:
                rouletteMachine.RaiseBet();
                break;

            case InteractableType.BetDown:
                rouletteMachine.LowerBet();
                break;
        }
    }
}
