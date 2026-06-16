using UnityEngine;
using UnityEngine.UI;

namespace FlightModel
{
    public class FlightTuningOverlay : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text profileLabel;
        [SerializeField] Slider massSlider;
        [SerializeField] Slider boostSlider;
        [SerializeField] Slider forwardThrustSlider;
        [SerializeField] Slider torqueSlider;
        [SerializeField] Dropdown profileDropdown;
        [SerializeField] Button applyButton;
        [SerializeField] Button saveButton;
        [SerializeField] Button resetButton;

        ShipFlightController flight;
        ShipTuning sourceAsset;
        ShipTuning runtimeCopy;
        ShipTuningProfileLibrary library;

        public void Initialize(ShipFlightController controller, ShipTuning asset, ShipTuningProfileLibrary profileLibrary)
        {
            flight = controller;
            sourceAsset = asset;
            library = profileLibrary;
            runtimeCopy = Instantiate(asset);
            flight.Tuning = runtimeCopy;
            ShipTuningJsonStore.TryLoad(runtimeCopy);

            PopulateProfiles();
            BindUi();
            SyncSlidersFromRuntime();
            Hide();
        }

        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public bool IsVisible => root != null && root.activeSelf;

        void PopulateProfiles()
        {
            profileDropdown.ClearOptions();
            if (library == null || library.profiles == null)
            {
                return;
            }

            var names = new System.Collections.Generic.List<string>();
            foreach (ShipTuning profile in library.profiles)
            {
                names.Add(profile != null ? profile.name : "Missing");
            }

            profileDropdown.AddOptions(names);
        }

        void BindUi()
        {
            applyButton.onClick.AddListener(ApplyFromSliders);
            saveButton.onClick.AddListener(SaveProfile);
            resetButton.onClick.AddListener(ResetToDefault);
            profileDropdown.onValueChanged.AddListener(OnProfileSelected);
        }

        void SyncSlidersFromRuntime()
        {
            massSlider.value = runtimeCopy.massKg;
            boostSlider.value = runtimeCopy.boostMultiplier;
            forwardThrustSlider.value = runtimeCopy.maxThrustNewtons.z;
            torqueSlider.value = runtimeCopy.maxTorque.x;
            if (profileLabel != null)
            {
                profileLabel.text = sourceAsset != null ? sourceAsset.name : "Runtime";
            }
        }

        void ApplyFromSliders()
        {
            runtimeCopy.massKg = massSlider.value;
            runtimeCopy.boostMultiplier = boostSlider.value;
            runtimeCopy.maxThrustNewtons.z = forwardThrustSlider.value;
            runtimeCopy.maxTorque = new Vector3(torqueSlider.value, torqueSlider.value, torqueSlider.value);
            flight.Tuning = runtimeCopy;
        }

        void SaveProfile()
        {
            ApplyFromSliders();
            ShipTuningJsonStore.Save(runtimeCopy);
        }

        void ResetToDefault()
        {
            if (sourceAsset == null)
            {
                return;
            }

            CopyTuning(sourceAsset, runtimeCopy);
            flight.Tuning = runtimeCopy;
            SyncSlidersFromRuntime();
        }

        void OnProfileSelected(int index)
        {
            if (library == null || library.profiles == null || index < 0 || index >= library.profiles.Length)
            {
                return;
            }

            CopyTuning(library.profiles[index], runtimeCopy);
            flight.Tuning = runtimeCopy;
            SyncSlidersFromRuntime();
        }

        static void CopyTuning(ShipTuning from, ShipTuning to)
        {
            to.massKg = from.massKg;
            to.maxThrustNewtons = from.maxThrustNewtons;
            to.maxTorque = from.maxTorque;
            to.boostMultiplier = from.boostMultiplier;
            to.angularDampingStrength = from.angularDampingStrength;
            to.brakeLinearDampingStrength = from.brakeLinearDampingStrength;
            to.brakeAngularDampingStrength = from.brakeAngularDampingStrength;
            to.coupledLateralDampingStrength = from.coupledLateralDampingStrength;
            to.frameLockLinearDampingStrength = from.frameLockLinearDampingStrength;
        }
    }
}
