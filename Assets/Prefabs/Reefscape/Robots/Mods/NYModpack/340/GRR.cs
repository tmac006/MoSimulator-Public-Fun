using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.GRR._340
{
    public class GRR : ReefscapeRobotBase
    {
        [Header("Robot Components")]
        [SerializeField] private GenericJoint wrist;
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint climber;

        [Header("Motion Controllers")]
        [SerializeField]  private GRRAutoAlign autoAlign;
        [SerializeField] private PidConstants wristPID;
        [SerializeField] private PidConstants climberPID;

        [Header("Setpoints")]
        [SerializeField] private float safeDistance = 1.6f;
        [SerializeField] private GRRSetpoint stow;
        [SerializeField] private GRRSetpoint coralIntake;
        [SerializeField] private GRRSetpoint coralStow;
        [SerializeField] private GRRSetpoint l1Left;
        [SerializeField] private GRRSetpoint l1Right;
        [SerializeField] private GRRSetpoint l2;
        [SerializeField] private GRRSetpoint l2Place;
        [SerializeField] private GRRSetpoint l3;
        [SerializeField] private GRRSetpoint l3Place;
        [SerializeField] private GRRSetpoint l4;
        [SerializeField] private GRRSetpoint l4Place;
        [SerializeField] private GRRSetpoint lowAlgae;
        [SerializeField] private GRRSetpoint highAlgae;
        [SerializeField] private GRRSetpoint climb;
        [SerializeField] private GRRSetpoint climbed;

        [Header("Game Piece Intakes")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntakeComponent;
        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;

        [Header("Audio")]
        [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeAudioClip;
        [SerializeField] private AudioSource gooseAudioSource;
        [SerializeField] private AudioClip gooseAudioClip;

        [Header("Goose Wheel Animation")]
        [SerializeField] private GenericAnimationJoint[] gooseAnimationWheels;
        [SerializeField] private float gooseAnimationWheelSpeed = 900f;

        [Header("Intake Wheel Animation")] 
        [SerializeField] private GenericAnimationJoint[] intakeAnimationWheels;
        [SerializeField] private float intakeAnimationWheelSpeed = 900f;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;

        private bool _alreadyPlaced = false;
        private float _placeAudioStartTime = -1f;

        private float _wristAngle;
        private float _elevatorDistance;
        private float _climberAngle;
        private GRRSetpoint _currentSetpoint;

        protected override void Start()
        {
            base.Start();

            wrist.SetPid(wristPID);
            climber.SetPid(climberPID);

            _currentSetpoint = stow;
            _wristAngle = stow.wristTarget;
            _elevatorDistance = stow.elevatorDistance;
            _climberAngle = stow.climberTarget;

            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _coralController.gamePieceStates = new[] { coralIntakeState, coralStowState };
            _coralController.intakes.Add(coralIntakeComponent);

            if (intakeAudioSource != null && intakeAudioClip != null)
            {
                intakeAudioSource.clip = intakeAudioClip;
                intakeAudioSource.loop = true;
                intakeAudioSource.Stop();
            }
            
            if (gooseAudioSource != null && gooseAudioClip != null)
            {
                gooseAudioSource.clip = gooseAudioClip;
                gooseAudioSource.loop = true;
                gooseAudioSource.Stop();
            }
        }

        private void LateUpdate()
        {
            wrist.UpdatePid(wristPID);
            climber.UpdatePid(climberPID);
        }

        private void FixedUpdate()
        {
            SetRobotMode(ReefscapeRobotMode.Coral); // hehe
            SetWheelSpeeds(0, 0);

            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (intakeAudioSource != null && intakeAudioSource.isPlaying)
                {
                    intakeAudioSource.Stop();
                }

                if (gooseAudioSource != null && gooseAudioSource.isPlaying)
                {
                    gooseAudioSource.Stop();
                }

                return;
            }

            bool hasCoral = _coralController.HasPiece();
            bool coralSeated = _coralController.atTarget;
            bool autoPlace = autoAlign.InPosition();
            bool safe = autoAlign.ReefDistance() >= safeDistance;

            if (autoPlace) SetState(ReefscapeSetpoints.Place);
            if (CurrentSetpoint != ReefscapeSetpoints.Place) _alreadyPlaced = false;

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stack:
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Stow:
                case ReefscapeSetpoints.Intake:
                    if (safe)
                    {
                        if (!hasCoral)
                        {
                            _coralController.SetTargetState(coralIntakeState);
                            _coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                            SetSetpoint(IntakeAction.IsPressed() ? coralIntake : stow);
                        }
                        else
                        {
                            if (IntakeAction.IsPressed()) _coralController.SetTargetState(coralStowState);
                            SetSetpoint(!coralSeated ? coralIntake : coralStow);
                        }
                
                        if (!coralSeated && IntakeAction.IsPressed())
                        {
                            SetWheelSpeeds(gooseAnimationWheelSpeed, intakeAnimationWheelSpeed);
                        }
                    }
                    
                    break;
                case ReefscapeSetpoints.Place:
                    if (!_alreadyPlaced && (OuttakeAction.triggered || autoPlace))
                    {
                        StartCoroutine(PlacePiece());
                        _alreadyPlaced = true;
                    }

                    SetWheelSpeeds(-gooseAnimationWheelSpeed, 0);
                    if (_alreadyPlaced && safe) SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.L1:
                    if (coralSeated) SetSetpoint(autoAlign.Left() ? l1Left : l1Right);
                    break;
                case ReefscapeSetpoints.L2:
                    if (coralSeated) SetSetpoint(l2);
                    else if (!hasCoral) SetState(ReefscapeSetpoints.LowAlgae);
                    break;
                case ReefscapeSetpoints.L3:
                    if (coralSeated) SetSetpoint(l3);
                    else if (!hasCoral) SetState(ReefscapeSetpoints.HighAlgae);
                    break;
                case ReefscapeSetpoints.L4:
                    if (coralSeated) SetSetpoint(l4);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(lowAlgae);
                    // Move rollers on algae setpoints (same as outtake)
                    SetWheelSpeeds(-gooseAnimationWheelSpeed, 0);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(highAlgae);
                    // Move rollers on algae setpoints (same as outtake)
                    SetWheelSpeeds(-gooseAnimationWheelSpeed, 0);
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climb);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            // Play funnel audio when actively intaking coral (same condition as intake wheels)
            bool isIntaking = (CurrentSetpoint == ReefscapeSetpoints.Stow || CurrentSetpoint == ReefscapeSetpoints.Intake) &&
                              !coralSeated && IntakeAction.IsPressed() && safe;
            
            if (isIntaking)
            {
                if (intakeAudioSource != null && !intakeAudioSource.isPlaying)
                {
                    intakeAudioSource.Play();
                }
            }
            else
            {
                if (intakeAudioSource != null && intakeAudioSource.isPlaying)
                {
                    intakeAudioSource.Stop();
                }
            }

            // Play goose audio when at algae setpoints or when actually scoring (Place setpoint)
            bool isAtAlgae = CurrentSetpoint == ReefscapeSetpoints.LowAlgae ||
                             CurrentSetpoint == ReefscapeSetpoints.HighAlgae;
            
            bool isScoring = CurrentSetpoint == ReefscapeSetpoints.Place;
            
            // Track when we enter Place setpoint for timing the audio
            if (isScoring && _placeAudioStartTime < 0)
            {
                _placeAudioStartTime = Time.time;
            }
            else if (!isScoring)
            {
                _placeAudioStartTime = -1f;
            }
            
            // Check if Place audio should still be playing (0.75 seconds)
            bool shouldPlayPlaceAudio = isScoring && 
                                        _placeAudioStartTime >= 0 && 
                                        (Time.time - _placeAudioStartTime) < 0.45f;
            
            if (isAtAlgae || shouldPlayPlaceAudio)
            {
                if (gooseAudioSource != null && !gooseAudioSource.isPlaying)
                {
                    gooseAudioSource.Play();
                }
            }
            else
            {
                if (gooseAudioSource != null && gooseAudioSource.isPlaying)
                {
                    gooseAudioSource.Stop();
                }
            }

            UpdateSetpoints();
        }

        private void SetSetpoint(GRRSetpoint setpoint)
        {
            _currentSetpoint = setpoint;
            _wristAngle = setpoint.wristTarget;
            _elevatorDistance = setpoint.elevatorDistance;
            _climberAngle = setpoint.climberTarget;
        }

        private void UpdateSetpoints()
        {
            var wristAngleCall = wrist.SetTargetAngle(_wristAngle).withAxis(JointAxis.X).flipDirection();
            if (_currentSetpoint != null && _currentSetpoint.wristNoWrapAngle != 360)
            {
                wristAngleCall.noWrap(_currentSetpoint.wristNoWrapAngle);
            }
            else
            {
                wrist.noWrap = false;
            }
            
            elevator.SetTarget(_elevatorDistance);
            
            var climberAngleCall = climber.SetTargetAngle(_climberAngle).withAxis(JointAxis.Z).flipDirection();
            if (_currentSetpoint != null && _currentSetpoint.climberNoWrapAngle != 360)
            {
                climberAngleCall.noWrap(_currentSetpoint.climberNoWrapAngle);
            }
            else
            {
                climber.noWrap = false;
            }
        }

        private IEnumerator PlacePiece()
        {
            if (_alreadyPlaced) yield break;
            
            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4Place);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3Place);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2Place);
                    break;
            }

            if (CurrentRobotMode == ReefscapeRobotMode.Coral && _coralController.HasPiece())
            {
                var time = 0.3f;
                var force = new Vector3(0, 0, 4f);
                var maxSpeed = 0.8f;

                if (LastSetpoint == ReefscapeSetpoints.L4 || LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    time = 0.35f;
                    force = new Vector3(0, 0.4f, 0.6f);
                    maxSpeed = 5f;
                }

                else if (LastSetpoint == ReefscapeSetpoints.L1)
                {
                    time = 0.4f;
                    force = new Vector3(0, 0f, 3f);
                    maxSpeed = 0.5f;
                }

                // Reverse force if coral is in stow position
                // Check if we came from Stow/Intake position and coral was in stow state
                bool isInStow = (LastSetpoint == ReefscapeSetpoints.Stow || LastSetpoint == ReefscapeSetpoints.Intake) &&
                                 _coralController.GetCurrentState() == coralStowState;
                
                if (isInStow)
                {
                    force = -force;
                }

                _coralController.ReleaseGamePieceWithContinuedForce(force, time, maxSpeed);
            }
            
            yield break;
        }

        private void SetWheelSpeeds(float goose, float intake)
        {
            for (int i = 0; i < gooseAnimationWheels.Length; i++)
            {
                gooseAnimationWheels[i].VelocityRoller(goose * (i < 3 ? -1 : 1));
            }

            for (int i = 0; i < intakeAnimationWheels.Length; i++)
            {
                intakeAnimationWheels[i].VelocityRoller(intake * (i < 2 ? 1 : -1));
            }
        }
    }
}

