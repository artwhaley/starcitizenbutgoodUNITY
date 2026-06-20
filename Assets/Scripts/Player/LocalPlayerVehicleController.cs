using FlightModel.Authority;
using UnityEngine;

namespace FlightModel
{
    public class LocalPlayerVehicleController : MonoBehaviour
    {
        [SerializeField] ShipInputReader input;
        [SerializeField] JoystickInputProvider joystickProvider;
        [SerializeField] InputBindingsPanel bindingsPanel;
        [SerializeField] ShipPilotSeat startingSeat;
        [SerializeField] bool enterStartingSeatOnStart = true;

        ShipPilotSeat currentSeat;

        ShipVehicle CurrentVehicle => currentSeat != null ? currentSeat.Vehicle : null;

        void Awake()
        {
            if (input == null)
            {
                input = GetComponent<ShipInputReader>();
            }

            if (joystickProvider == null)
            {
                joystickProvider = GetComponent<JoystickInputProvider>();
            }

            if (bindingsPanel == null)
            {
                bindingsPanel = FindAnyObjectByType<InputBindingsPanel>(FindObjectsInactive.Include);
            }

            if (input != null && joystickProvider != null)
            {
                input.SetJoystickProvider(joystickProvider);
            }

            WireInputCallbacks();
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (enterStartingSeatOnStart && startingSeat != null)
            {
                EnterSeat(startingSeat);
            }
            else if (enterStartingSeatOnStart)
            {
                ShipPilotSeat foundSeat = GetComponentInChildren<ShipPilotSeat>(true);
                if (foundSeat != null)
                {
                    EnterSeat(foundSeat);
                }
            }
        }

        void OnDestroy()
        {
            UnwireInputCallbacks();
        }

        void FixedUpdate()
        {
            ShipVehicle vehicle = CurrentVehicle;
            if (vehicle == null || input == null || !vehicle.HasLocalPilot)
            {
                return;
            }

            ShipInputCommand command = ResolveFlightCommand();
            vehicle.SubmitLocalInput(new ClientInputCommand
            {
                clientId = 1,
                controlledEntityId = vehicle.ControlledEntityId,
                inputTick = vehicle.ServerTick,
                shipInput = command
            });
        }

        void Update()
        {
            ShipVehicle vehicle = CurrentVehicle;
            if (vehicle == null || input == null || !vehicle.HasLocalPilot)
            {
                return;
            }

            vehicle.TickLocalPresentation(ResolveFlightCommand(), Time.deltaTime);
        }

        public bool EnterSeat(ShipPilotSeat seat)
        {
            if (seat == null)
            {
                return false;
            }

            if (currentSeat == seat)
            {
                return true;
            }

            ExitSeat();
            if (!seat.TryEnterLocalPilot())
            {
                return false;
            }

            currentSeat = seat;
            return true;
        }

        public void ExitSeat()
        {
            if (currentSeat == null)
            {
                return;
            }

            ShipPilotSeat oldSeat = currentSeat;
            currentSeat = null;
            oldSeat.ExitLocalPilot();
        }

        ShipInputCommand ResolveFlightCommand()
        {
            if (bindingsPanel != null && bindingsPanel.IsVisible)
            {
                return new ShipInputCommand
                {
                    fineControl = input != null && input.FineControlModeActive
                };
            }

            return input != null ? input.LastCommand : default;
        }

        void WireInputCallbacks()
        {
            if (input == null)
            {
                return;
            }

            input.ToggleAssistRequested += HandleToggleAssist;
            input.ToggleCameraRequested += HandleToggleCamera;
            input.ToggleBindingsPanelRequested += HandleToggleBindingsPanel;
            input.ToggleDockingModeRequested += HandleToggleDockingMode;
            input.UndockRequested += HandleUndockRequest;
            input.ToggleDebugOverlayRequested += HandleToggleDebugOverlay;
        }

        void UnwireInputCallbacks()
        {
            if (input == null)
            {
                return;
            }

            input.ToggleAssistRequested -= HandleToggleAssist;
            input.ToggleCameraRequested -= HandleToggleCamera;
            input.ToggleBindingsPanelRequested -= HandleToggleBindingsPanel;
            input.ToggleDockingModeRequested -= HandleToggleDockingMode;
            input.UndockRequested -= HandleUndockRequest;
            input.ToggleDebugOverlayRequested -= HandleToggleDebugOverlay;
        }

        void HandleToggleAssist() => CurrentVehicle?.CycleAssistMode();
        void HandleToggleCamera() => CurrentVehicle?.ToggleCameraView();
        void HandleToggleBindingsPanel() => input?.HandleBindingsPanelToggle(bindingsPanel);
        void HandleToggleDockingMode() => CurrentVehicle?.ToggleDockingMode();
        void HandleUndockRequest() => CurrentVehicle?.HandleUndockRequest();
        void HandleToggleDebugOverlay() => CurrentVehicle?.ToggleDebugOverlay();
    }
}
