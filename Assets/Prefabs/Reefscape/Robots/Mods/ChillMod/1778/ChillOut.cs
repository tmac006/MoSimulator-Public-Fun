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

namespace Prefabs.Reefscape.Robots.Mods.ChillMod._1778
{
    public class ChillOut : ReefscapeRobotBase
    {
        [Header("Robot Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint intakeJoint;
        [SerializeField] private GenericJoint clawJoint;

        [Header("PID Constants")]
        [SerializeField] private PidConstants intakePidConstants;
        [SerializeField] private PidConstants clawPidConstants;

        [Header("Stow Setpoints")]
        [SerializeField] private ChillOutSetpoint stowSetpoint;
        [SerializeField] private ChillOutSetpoint coralStowSetpoint;
        [SerializeField] private ChillOutSetpoint algaeStowSetpoint;

        [Header("Intake Setpoints")]
        [SerializeField] private ChillOutSetpoint coralIntakeSetpoint;
        [SerializeField] private ChillOutSetpoint algaeGroundIntakeSetpoint;
        [SerializeField] private ChillOutSetpoint stackAlgaeIntakeSetpoint;

        [Header("Coral Scoring Setpoints - Front")]
        [SerializeField] private ChillOutSetpoint l1Setpoint;
        [SerializeField] private ChillOutSetpoint l2Setpoint;
        [SerializeField] private ChillOutSetpoint l3Setpoint;
        [SerializeField] private ChillOutSetpoint l4Setpoint;

        [Header("Coral Scoring Setpoints - Back")]
        [SerializeField] private ChillOutSetpoint l2BackSetpoint;
        [SerializeField] private ChillOutSetpoint l3BackSetpoint;
        [SerializeField] private ChillOutSetpoint l4BackSetpoint;

        [Header("Coral Place Setpoints")]
        [SerializeField] private ChillOutSetpoint l2PlaceSetpoint;
        [SerializeField] private ChillOutSetpoint l3PlaceSetpoint;
        [SerializeField] private ChillOutSetpoint l4PlaceSetpoint;
        [SerializeField] private ChillOutSetpoint l4BackPlaceSetpoint;

        [Header("Algae Setpoints - Front")]
        [SerializeField] private ChillOutSetpoint lowAlgaeSetpoint;
        [SerializeField] private ChillOutSetpoint highAlgaeSetpoint;

        [Header("Algae Setpoints - Back")]
        [SerializeField] private ChillOutSetpoint lowAlgaeBackSetpoint;
        [SerializeField] private ChillOutSetpoint highAlgaeBackSetpoint;

        [Header("Barge Setpoints")]
        [SerializeField] private ChillOutSetpoint bargeSetpoint;
        [SerializeField] private ChillOutSetpoint bargeBackSetpoint;
        [SerializeField] private ChillOutSetpoint bargePlaceSetpoint;

        [Header("Processor Setpoint")]
        [SerializeField] private ChillOutSetpoint processorSetpoint;

        [Header("Climb Setpoints")]
        [SerializeField] private ChillOutSetpoint climbSetpoint;
        [SerializeField] private ChillOutSetpoint climbedSetpoint;

        [Header("Game Piece Intakes")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Game Piece States")]
        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState coralFrontStowState;
        [SerializeField] private GamePieceState coralBackStowState;
        [SerializeField] private GamePieceState algaeStowState;

        [Header("Release Forces")]
        [SerializeField] private float coralEjectForce = 5f;
        [SerializeField] private float algaeEjectForce = 4f;

        [Header("Animation Wheels")]
        [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private float intakeWheelSpeed = 500f;

        [Header("Audio")]
        [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeClip;
        [SerializeField] private AudioSource algaeStallSource;
        [SerializeField] private AudioClip algaeStallClip;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private float _targetElevatorHeight;
        private float _targetIntakeAngle;
        private float _targetClawAngle;

        private bool _isScoring;
        private bool _alreadyPlaced;

        protected override void Start()
        {
            base.Start();

            if (intakeJoint != null && intakePidConstants != null)
                intakeJoint.SetPid(intakePidConstants);
            if (clawJoint != null && clawPidConstants != null)
                clawJoint.SetPid(clawPidConstants);

            _targetElevatorHeight = stowSetpoint != null ? stowSetpoint.elevatorHeight : 0f;
            _targetIntakeAngle = stowSetpoint != null ? stowSetpoint.intakeAngle : 0f;
            _targetClawAngle = stowSetpoint != null ? stowSetpoint.clawAngle : 0f;

            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[] { coralIntakeState, coralStowState, coralFrontStowState, coralBackStowState };
            _coralController.intakes.Add(coralIntake);

            _algaeController.gamePieceStates = new[] { algaeStowState };
            _algaeController.intakes.Add(algaeIntake);

            _isScoring = false;
            _alreadyPlaced = false;

            if (intakeAudioSource != null && intakeClip != null)
            {
                intakeAudioSource.clip = intakeClip;
                intakeAudioSource.loop = true;
                intakeAudioSource.playOnAwake = false;
            }

            if (algaeStallSource != null && algaeStallClip != null)
            {
                algaeStallSource.clip = algaeStallClip;
                algaeStallSource.loop = true;
                algaeStallSource.playOnAwake = false;
            }
        }

        private void LateUpdate()
        {
            if (intakeJoint != null && intakePidConstants != null)
                intakeJoint.UpdatePid(intakePidConstants);
            if (clawJoint != null && clawPidConstants != null)
                clawJoint.UpdatePid(clawPidConstants);
        }

        private void FixedUpdate()
        {
            bool hasCoral = _coralController.HasPiece();
            bool hasAlgae = _algaeController.HasPiece();
            bool canIntakeCoral = !hasCoral && !hasAlgae && IntakeAction.IsPressed();
            bool canIntakeAlgae = !hasCoral && !hasAlgae && IntakeAction.IsPressed();

            _algaeController.SetTargetState(algaeStowState);

            if (CurrentSetpoint != ReefscapeSetpoints.Place)
                _alreadyPlaced = false;

            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                StopAudio();
                UpdateSetpoints();
                return;
            }

            // Handle wheel animation
            if (!_isScoring)
            {
                bool isIntaking = (CurrentSetpoint == ReefscapeSetpoints.Intake || CurrentSetpoint == ReefscapeSetpoints.Stack) && IntakeAction.IsPressed();
                float wheelSpeed = isIntaking ? intakeWheelSpeed : 0f;
                
                foreach (var wheel in intakeWheels)
                {
                    if (wheel != null)
                        wheel.VelocityRoller(wheelSpeed).useAxis(JointAxis.X);
                }
            }

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (hasCoral)
                    {
                        SetSetpoint(coralStowSetpoint);
                        _coralController.SetTargetState(coralStowState);
                    }
                    else if (hasAlgae)
                    {
                        SetSetpoint(algaeStowSetpoint);
                    }
                    else
                    {
                        SetSetpoint(stowSetpoint);
                        _coralController.SetTargetState(coralStowState);
                    }
                    break;

                case ReefscapeSetpoints.Intake:
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetSetpoint(coralIntakeSetpoint);
                        _coralController.SetTargetState(coralIntakeState);
                        _coralController.RequestIntake(coralIntake, canIntakeCoral);
                    }
                    else
                    {
                        SetSetpoint(algaeGroundIntakeSetpoint);
                        _algaeController.RequestIntake(algaeIntake, canIntakeAlgae);
                    }
                    break;

                case ReefscapeSetpoints.Place:
                    if (!_alreadyPlaced)
                    {
                        StartCoroutine(PlaceGamePiece());
                        _alreadyPlaced = true;
                    }
                    break;

                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1Setpoint);
                    break;

                case ReefscapeSetpoints.L2:
                    SetSetpoint(FacingReef ? l2Setpoint : l2BackSetpoint);
                    _coralController.SetTargetState(FacingReef ? coralFrontStowState : coralBackStowState);
                    break;

                case ReefscapeSetpoints.L3:
                    SetSetpoint(FacingReef ? l3Setpoint : l3BackSetpoint);
                    _coralController.SetTargetState(FacingReef ? coralFrontStowState : coralBackStowState);
                    break;

                case ReefscapeSetpoints.L4:
                    SetSetpoint(FacingReef ? l4Setpoint : l4BackSetpoint);
                    _coralController.SetTargetState(FacingReef ? coralFrontStowState : coralBackStowState);
                    break;

                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? lowAlgaeSetpoint : lowAlgaeBackSetpoint);
                    _algaeController.RequestIntake(algaeIntake, canIntakeAlgae);
                    break;

                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(FacingReef ? highAlgaeSetpoint : highAlgaeBackSetpoint);
                    _algaeController.RequestIntake(algaeIntake, canIntakeAlgae);
                    break;

                case ReefscapeSetpoints.Stack:
                    SetSetpoint(stackAlgaeIntakeSetpoint);
                    _algaeController.RequestIntake(algaeIntake, canIntakeAlgae);
                    break;

                case ReefscapeSetpoints.Barge:
                    SetSetpoint(FacingReef ? bargeSetpoint : bargeBackSetpoint);
                    break;

                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processorSetpoint);
                    break;

                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climbSetpoint);
                    break;

                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbedSetpoint);
                    break;

                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            UpdateSetpoints();
            UpdateAudio();
        }

        private IEnumerator PlaceGamePiece()
        {
            _isScoring = true;

            // Spin wheels for outtake
            float speed = FacingReef ? intakeWheelSpeed : -intakeWheelSpeed;
            foreach (var wheel in intakeWheels)
            {
                if (wheel != null)
                    wheel.VelocityRoller(speed).useAxis(JointAxis.X);
            }

            // Apply place setpoint based on last setpoint
            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L4:
                    SetSetpoint(FacingReef ? l4PlaceSetpoint : l4BackPlaceSetpoint);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3PlaceSetpoint);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2PlaceSetpoint);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetSetpoint(bargePlaceSetpoint);
                    break;
            }

            yield return new WaitForSeconds(0.05f);

            // Release game pieces
            if (_coralController.HasPiece())
            {
                Vector3 coralForce;
                if (LastSetpoint == ReefscapeSetpoints.L1)
                {
                    coralForce = new Vector3(coralEjectForce * 0.5f, 0, 0);
                }
                else
                {
                    coralForce = FacingReef 
                        ? new Vector3(0, 0, -coralEjectForce) 
                        : new Vector3(0, 0, coralEjectForce);
                }
                _coralController.ReleaseGamePieceWithForce(coralForce);
            }

            if (_algaeController.HasPiece())
            {
                Vector3 algaeForce;
                if (LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    algaeForce = new Vector3(0, algaeEjectForce * 2.5f, 0);
                }
                else
                {
                    algaeForce = new Vector3(0, algaeEjectForce, 0);
                }
                _algaeController.ReleaseGamePieceWithForce(algaeForce);
            }

            // Wait for release
            float timer = 0f;
            while ((_coralController.currentStateNum != 0 || _algaeController.currentStateNum != 0) && timer < 0.5f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Stop wheels
            foreach (var wheel in intakeWheels)
            {
                if (wheel != null)
                    wheel.VelocityRoller(0).useAxis(JointAxis.X);
            }

            _isScoring = false;
        }

        private void SetSetpoint(ChillOutSetpoint setpoint)
        {
            if (setpoint == null) return;
            
            _targetElevatorHeight = setpoint.elevatorHeight;
            _targetIntakeAngle = setpoint.intakeAngle;
            _targetClawAngle = setpoint.clawAngle;
        }

        private void UpdateSetpoints()
        {
            if (elevator != null)
                elevator.SetTarget(_targetElevatorHeight);
            
            if (intakeJoint != null)
                intakeJoint.SetTargetAngle(_targetIntakeAngle).withAxis(JointAxis.X);
            
            if (clawJoint != null)
                clawJoint.SetTargetAngle(_targetClawAngle).withAxis(JointAxis.X);
        }

        private void UpdateAudio()
        {
            if (intakeAudioSource == null || algaeStallSource == null) return;

            // Intake audio
            if (IntakeAction.IsPressed() && !_coralController.HasPiece() && !_algaeController.HasPiece())
            {
                if (!intakeAudioSource.isPlaying)
                    intakeAudioSource.Play();
            }
            else
            {
                if (intakeAudioSource.isPlaying)
                    intakeAudioSource.Stop();
            }

            // Algae stall audio
            if (_algaeController.HasPiece())
            {
                if (!algaeStallSource.isPlaying)
                    algaeStallSource.Play();
            }
            else
            {
                if (algaeStallSource.isPlaying)
                    algaeStallSource.Stop();
            }
        }

        private void StopAudio()
        {
            if (intakeAudioSource != null && intakeAudioSource.isPlaying)
                intakeAudioSource.Stop();
            if (algaeStallSource != null && algaeStallSource.isPlaying)
                algaeStallSource.Stop();
        }
    }
}

