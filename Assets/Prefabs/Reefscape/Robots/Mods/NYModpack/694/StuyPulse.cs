using System;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using JetBrains.Annotations;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework.Components;
using RobotFramework.Controllers.Drivetrain;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.NYPowerhousePack._694
{
    public class StuyPulse: ReefscapeRobotBase
    {
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint eeArm;
        [SerializeField] private GenericJoint froggy;
        [SerializeField] private GenericJoint climbPivot1;
        [SerializeField] private GenericJoint climbPivot2;

        
        [Header("PID Constants")]
        [SerializeField] private PidConstants eeArmPid;
        [SerializeField] private PidConstants froggyPid;
        [SerializeField] private PidConstants climbPivotsPid;

        
        [Header("Setpoints")]
        [SerializeField] private StuyPulseSetpoint stow;
        [SerializeField] private StuyPulseSetpoint intakeFunnel;
        [SerializeField] private StuyPulseSetpoint eeL1;
        [SerializeField] private StuyPulseSetpoint frontL2;
        [SerializeField] private StuyPulseSetpoint backL2;
        [SerializeField] private StuyPulseSetpoint frontL3;
        [SerializeField] private StuyPulseSetpoint backL3;
        [SerializeField] private StuyPulseSetpoint frontL4;
        [SerializeField] private StuyPulseSetpoint backL4;
        [SerializeField] private StuyPulseSetpoint backL4Scored;

        [SerializeField] private StuyPulseSetpoint lollipopIntake;
        [SerializeField] private StuyPulseSetpoint frontLowAlgae;
        [SerializeField] private StuyPulseSetpoint frontHighAlgae;
        [SerializeField] private StuyPulseSetpoint backLowAlgae;
        [SerializeField] private StuyPulseSetpoint backHighAlgae;
        [SerializeField] private StuyPulseSetpoint bargePrep;
        [SerializeField] private StuyPulseSetpoint bargePlace;
        [SerializeField] private StuyPulseSetpoint process;

        [SerializeField] private StuyPulseSetpoint froggyCoral;
        [SerializeField] private StuyPulseSetpoint froggyAlgae;
        [SerializeField] private StuyPulseSetpoint froggyLollipop;
        [SerializeField] private StuyPulseSetpoint froggyCoralPlace;
        [SerializeField] private StuyPulseSetpoint froggyAlgaeProcess;

        [SerializeField] private StuyPulseSetpoint climbStow;
        [SerializeField] private StuyPulseSetpoint climbPrep;
        [SerializeField] private StuyPulseSetpoint climbClimb;

        [Header("Intakes and Stows")] 
        [SerializeField] private ReefscapeGamePieceIntake funnelCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake shooterAlgaeIntake;
        [SerializeField] private ReefscapeGamePieceIntake froggyCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake froggyAlgaeIntake;

        [SerializeField] private GamePieceState shooterCoralStowState;
        [SerializeField] private GamePieceState shooterAlgaeStowState;
        
        [SerializeField] private GamePieceState froggyCoralStowState;
        [SerializeField] private GamePieceState froggyAlgaeStowState;

        [Header("Auto Align Offsets")] 
        [SerializeField] private AutoAlignOffset frontLeftOffset;
        [SerializeField] private AutoAlignOffset frontRightOffset;
        [SerializeField] private AutoAlignOffset backLeftOffset;
        [SerializeField] private AutoAlignOffset backRightOffset;
        [SerializeField] private AutoAlignOffset frontLeftL4Offset;
        [SerializeField] private AutoAlignOffset frontRightL4Offset;
        [SerializeField] private AutoAlignOffset backLeftL4Offset;
        [SerializeField] private AutoAlignOffset backRightL4Offset;
        
        [SerializeField] private AutoAlignOffset L1FroggyOffset;

        [Header("Froggy Shit")]
        [SerializeField] private Transform forggyCoralTarget;
        [SerializeField] private Transform frogyCoralSlid;
        [SerializeField] private Transform froggyAlgaeTarger;
        [SerializeField] private Transform froggyAlgaeSlider;

        [Header("Random Release Shit")] 
        [SerializeField] private ReleaseVector shooterProcRelease;
        [SerializeField] private ReleaseVector shooterNetRelease;
        [SerializeField] private ReleaseVector ForgyProcRelease;
        
        [SerializeField] private ReleaseVector forgCorlaScor;
        
        [Header("Rollers n Other Stuff ig")]
        [SerializeField] private GenericRoller[] froggyRollers;
        [SerializeField] private GenericRoller[] funnelRollers;

        [Header("Colliders n shit")]
        [SerializeField] private CapsuleCollider[] froggyRollerColliders;
        [SerializeField] private MeshCollider[] shooterCollidersForAlgae;
        
        [Header("Audio Stuff")]
        [SerializeField] private AudioSource funnelBelt;
        [SerializeField] private AudioSource funnelIntake;

        [SerializeField] private AudioClip beltRunning;
        [SerializeField] private AudioClip intake;
        
        
        
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode
            _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode
            _algaeController;

        private float _elevatorTargetHeight;
        private float _eeArmTargetAngle;
        private float _froggyTargetAngle;
        private float _climbPivot1TargetAngle;
        private float _climbPivot2TargetAngle;

        private ReefscapeAutoAlign align;

        private bool stillInPlaceState = false;

        private bool froggyLolli = false;
        
        protected override void Start()
        {
            base.Start();
            SetRobotMode(ReefscapeRobotMode.Coral);
            
            eeArm.SetPid(eeArmPid);
            froggy.SetPid(froggyPid);
            climbPivot1.SetPid(climbPivotsPid);
            climbPivot2.SetPid(climbPivotsPid);

            _elevatorTargetHeight = 0;
            _eeArmTargetAngle = 0;
            _froggyTargetAngle = 0;
            _climbPivot1TargetAngle = 0;
            _climbPivot2TargetAngle = 0;
            
            RobotGamePieceController.SetPreload(shooterCoralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                shooterCoralStowState, 
                froggyCoralStowState
            };
            _coralController.intakes.Add(funnelCoralIntake);
            _coralController.intakes.Add(froggyCoralIntake);

            _algaeController.gamePieceStates = new[]
            {
                shooterAlgaeStowState,
                froggyAlgaeStowState
            };
            _algaeController.intakes.Add(shooterAlgaeIntake);
            _algaeController.intakes.Add(froggyAlgaeIntake);
            
            align = gameObject.GetComponent<ReefscapeAutoAlign>();
            
            funnelBelt.clip = beltRunning;
            funnelBelt.loop = true;
            funnelBelt.Stop();
            
            funnelIntake.clip = intake;
            funnelIntake.loop = true;
            funnelIntake.Stop();
        }

        private void SetSetpoint(StuyPulseSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _eeArmTargetAngle = setpoint.eeArmAngle;
            _froggyTargetAngle = setpoint.froggyAngle;
            _climbPivot1TargetAngle = setpoint.climbPivot1Angle;
            _climbPivot2TargetAngle = setpoint.climbPivot2Angle;
        }

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            eeArm.SetTargetAngle(_eeArmTargetAngle).withAxis(JointAxis.X).noWrap(20);
            froggy.SetTargetAngle(_froggyTargetAngle).withAxis(JointAxis.X).noWrap(-110);
            climbPivot1.SetTargetAngle(_climbPivot1TargetAngle).withAxis(JointAxis.X).noWrap(140);
            climbPivot2.SetTargetAngle(-1 * _climbPivot2TargetAngle).withAxis(JointAxis.X).noWrap(-140);
        }

        private void LateUpdate()
        {
            eeArm.UpdatePid(eeArmPid);
            froggy.UpdatePid(froggyPid);
            climbPivot1.UpdatePid(climbPivotsPid);
            climbPivot2.UpdatePid(climbPivotsPid);
        }

        private void SetAlignOffsets(AutoAlignOffset alignment)
        {
            align.offset = new Vector3(alignment.xOffset, alignment.yOffset, alignment.zOffset);
            align.rotation = alignment.Rotation;
        }

        private void AutoAlignnnn()
        {
            // if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && _coralController.currentStateNum == froggyCoralStowState.stateNum && _coralController.atTarget)
            // {
            //     SetAlignOffsets(L1FroggyOffset);
            // }
            if (AutoAlignLeftAction.IsPressed() && FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? frontLeftL4Offset : frontLeftOffset);
            }
            else if (AutoAlignRightAction.IsPressed() && FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? frontRightL4Offset : frontRightOffset);
            }
            else if (AutoAlignLeftAction.IsPressed() && !FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? backLeftL4Offset : backLeftOffset);
            }
            else if (AutoAlignRightAction.IsPressed() && !FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? backRightL4Offset : backRightOffset);
            }
        }

        private void PlacePiece()
        {
            if ((CurrentRobotMode == ReefscapeRobotMode.Coral || !_algaeController.atTarget) && LastSetpoint != ReefscapeSetpoints.L2 && LastSetpoint != ReefscapeSetpoints.L3 && LastSetpoint != ReefscapeSetpoints.L4 && _coralController.HasPiece() && !(_coralController.currentStateNum == shooterCoralStowState.stateNum && _coralController.atTarget))
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(forgCorlaScor.x, forgCorlaScor.y, forgCorlaScor.z));
            }
            else if ((CurrentRobotMode == ReefscapeRobotMode.Algae || !_coralController.atTarget) && _algaeController.HasPiece())
            {
                if (_algaeController.currentStateNum == shooterAlgaeStowState.stateNum && LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    foreach (var col in shooterCollidersForAlgae)
                    {
                        col.enabled = false;
                    }
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(shooterNetRelease.x, shooterNetRelease.y, shooterNetRelease.z));
                } else if (_algaeController.currentStateNum == shooterAlgaeStowState.stateNum && LastSetpoint == ReefscapeSetpoints.Processor)
                {
                    foreach (var col in shooterCollidersForAlgae)
                    {
                        col.enabled = false;
                    }
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(shooterProcRelease.x, shooterProcRelease.y, shooterProcRelease.z));
                }
                else
                {
                    foreach (var col in froggyRollerColliders)
                    {
                        col.enabled = false;
                    }
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(ForgyProcRelease.x, ForgyProcRelease.y, ForgyProcRelease.z));
                }
            }
            else if ((CurrentRobotMode == ReefscapeRobotMode.Coral || !_algaeController.atTarget) && LastSetpoint == ReefscapeSetpoints.L4)
            {
                _coralController.ReleaseGamePieceWithForce(FacingReef
                                                            ? new Vector3(0, 0, -6)
                                                            : new Vector3(0, 0, 5));
            }
            else if ((CurrentRobotMode == ReefscapeRobotMode.Coral || _algaeController.atTarget) && LastSetpoint == ReefscapeSetpoints.L1 && CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 3.5f), 0.2f, .9f);
            }
            else if (CurrentRobotMode == ReefscapeRobotMode.Coral || !_algaeController.atTarget)
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5));
            }

            stillInPlaceState = true;
        }

        private void UpdateAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (funnelBelt.isPlaying || funnelIntake.isPlaying)
                {
                    funnelBelt.Stop();
                    funnelIntake.Stop();
                }

                return;
            }

            if (IntakeAction.IsPressed() && !_coralController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Coral && !funnelBelt.isPlaying)
            {
                funnelBelt.Play();
            } else if (!IntakeAction.IsPressed() && funnelBelt.isPlaying)
            {
                funnelBelt.Stop();
            }
        }
        
        private void FixedUpdate()
        {
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();
            
            bool shooterHasAlgae = (_algaeController.currentStateNum == shooterAlgaeStowState.stateNum && _algaeController.atTarget);
            bool shooterHasCoral = (_coralController.currentStateNum == shooterCoralStowState.stateNum && _coralController.atTarget);

            if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
            {
                foreach (var roller in funnelRollers)
                {
                    roller.flipVelocity();
                }
            }
            
            if (froggyCoralIntake.GamePiece != null)
            {
                var localSliderSpaceZ = forggyCoralTarget.transform.InverseTransformPoint(froggyCoralIntake.GamePiece.transform.position).z;
                frogyCoralSlid.localPosition = new Vector3(0, 0, localSliderSpaceZ);
            }
            
            if (froggyAlgaeIntake.GamePiece != null)
            {
                var localSliderSpaceX = froggyAlgaeTarger.transform.InverseTransformPoint(froggyAlgaeIntake.GamePiece.transform.position).x;
                froggyAlgaeSlider.localPosition = new Vector3(localSliderSpaceX, 0, 0);
            }

            if (CurrentSetpoint == ReefscapeSetpoints.Intake && CurrentRobotMode == ReefscapeRobotMode.Algae)
            {
                foreach (var roller in froggyRollers)
                {
                    roller.flipVelocity();;
                }
            }

            if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && (CurrentSetpoint == ReefscapeSetpoints.Place || LastSetpoint == ReefscapeSetpoints.Place || CurrentSetpoint == ReefscapeSetpoints.L1))
            {
                foreach (var col in froggyRollerColliders)
                {
                    col.enabled = false;
                }
            }
            else
            {
                foreach (var col in froggyRollerColliders)
                {
                    col.enabled = true;
                }
            }

            if (hasCoral && !shooterHasCoral)
            {
                foreach (var roller in froggyRollers)
                {
                    roller.stopAngularVelocity();
                }
            }

            if (CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                stillInPlaceState = false;
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    foreach (var col in froggyRollerColliders)
                    {
                        col.enabled = false;
                    }
                    _coralController.RequestIntake(funnelCoralIntake, CurrentIntakeMode != ReefscapeIntakeMode.L1);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    foreach (var col in shooterCollidersForAlgae)
                    {
                        col.enabled = true;
                    }
                    foreach (var col in froggyRollerColliders)
                    {
                        col.enabled = true;
                    }
                    break;
                case ReefscapeSetpoints.Intake:

                    if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && !hasCoral && CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetSetpoint(froggyCoral);
                        _coralController.SetTargetState(froggyCoralStowState);
                        _coralController.RequestIntake(froggyCoralIntake);
                        _coralController.RequestIntake(funnelCoralIntake, false);
                    }
                    else if (CurrentRobotMode == ReefscapeRobotMode.Coral && !hasCoral && !shooterHasAlgae)
                    {
                        SetSetpoint(intakeFunnel);
                        _coralController.SetTargetState(shooterCoralStowState);
                        _coralController.RequestIntake(funnelCoralIntake);
                        _coralController.RequestIntake(froggyCoralIntake, false);
                    }
                    else if (!hasCoral && (!hasAlgae || (hasAlgae && !shooterHasAlgae)) && (LastSetpoint == ReefscapeSetpoints.HighAlgae || LastSetpoint == ReefscapeSetpoints.LowAlgae || LastSetpoint == ReefscapeSetpoints.Stack))
                    {
                        _algaeController.SetTargetState(shooterAlgaeStowState);
                        _algaeController.RequestIntake(shooterAlgaeIntake);
                        _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    }
                    else if (CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae)
                    {
                        SetSetpoint(froggyLolli ? froggyLollipop : froggyAlgae);
                        _algaeController.SetTargetState(froggyAlgaeStowState);
                        _algaeController.RequestIntake(froggyAlgaeIntake);
                        _algaeController.RequestIntake(shooterAlgaeIntake, false);
                    }
                    
                    break;
                case ReefscapeSetpoints.Place:
                    if (!stillInPlaceState)
                    {
                        if (shooterHasAlgae && LastSetpoint == ReefscapeSetpoints.Barge)
                        {
                            SetSetpoint(bargePlace);
                        }
                        else if (shooterHasCoral && LastSetpoint == ReefscapeSetpoints.L4)
                        {
                            SetSetpoint(FacingReef ? frontL4 : backL4Scored);
                        }

                        PlacePiece();
                    }

                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(shooterHasCoral? eeL1 : froggyCoralPlace);

                    _algaeController.RequestIntake(funnelCoralIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    foreach (var col in shooterCollidersForAlgae)
                    {
                        col.enabled = true;
                    }
                    break;
                case ReefscapeSetpoints.Stack:
                    if (!shooterHasCoral)
                    {
                        SetSetpoint(lollipopIntake);
                        _algaeController.SetTargetState(shooterAlgaeStowState);
                        _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                        _algaeController.RequestIntake(froggyAlgaeIntake, false);
                        foreach (var col in shooterCollidersForAlgae)
                        {
                            col.enabled = true;
                        }
                    }

                    break;
                case ReefscapeSetpoints.L2:
                    if (shooterHasCoral)
                    {
                        SetSetpoint(FacingReef ? frontL2 : backL2);
                    }
                    _algaeController.RequestIntake(funnelCoralIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    foreach (var col in shooterCollidersForAlgae)
                    {
                        col.enabled = true;
                    }
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    if (!shooterHasCoral)
                    {
                        SetSetpoint(FacingReef ? frontLowAlgae : backLowAlgae);
                        _algaeController.SetTargetState(shooterAlgaeStowState);
                        _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                        _algaeController.RequestIntake(froggyAlgaeIntake, false);
                        foreach (var col in shooterCollidersForAlgae)
                        {
                            col.enabled = true;
                        }
                    }

                    break;
                case ReefscapeSetpoints.L3:
                    if (shooterHasCoral)
                    {
                        SetSetpoint(FacingReef ? frontL3 : backL3);
                    }
                    _algaeController.RequestIntake(funnelCoralIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    if (!shooterHasCoral)
                    {
                        SetSetpoint(FacingReef ? frontHighAlgae : backHighAlgae);
                        _algaeController.SetTargetState(shooterAlgaeStowState);
                        _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                        _algaeController.RequestIntake(froggyAlgaeIntake, false);
                        foreach (var col in shooterCollidersForAlgae)
                        {
                            col.enabled = true;
                        }
                    }

                    break;
                case ReefscapeSetpoints.L4:
                    _algaeController.RequestIntake(funnelCoralIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    if (shooterHasCoral)
                    {
                        SetSetpoint(FacingReef ? frontL4 : backL4);
                    }
                    foreach (var col in shooterCollidersForAlgae)
                    {
                        col.enabled = true;
                    }

                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(shooterHasAlgae ? process : froggyAlgaeProcess);
                    _algaeController.RequestIntake(shooterAlgaeIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.Barge:
                    if (shooterHasAlgae)
                    {
                        SetSetpoint(bargePrep);
                    }
                    _algaeController.RequestIntake(shooterAlgaeIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    froggyLolli = !froggyLolli;
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climbPrep);
                    _algaeController.RequestIntake(shooterAlgaeIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbClimb);
                    break;
            }
            
            UpdateSetpoints();
            AutoAlignnnn();
            // UpdateAudio();
        }


    }
    
    [Serializable]
    public struct ReleaseVector
    {
        [Tooltip("Units idk")] public int x;
        [Tooltip("Units idk")] public int y;
        [Tooltip("Units idk")] public int z;
    }

}