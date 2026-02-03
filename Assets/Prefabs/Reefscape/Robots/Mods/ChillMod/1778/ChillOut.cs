using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using Unity.VisualScripting;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.ChillMod._1778
{
    public class ChillOut: ReefscapeRobotBase
    {
        [Header("Joints")]
        [SerializeField] private GenericElevator elevator;

        [SerializeField] private GenericJoint arm;
        [SerializeField] private PidConstants armPid;
        
        [SerializeField] private GenericJoint intake;
        [SerializeField] private PidConstants intakePid;

        private float _elevatorTargetHeight;
        private float _armTargetAngle;
        private float _intakeTargetAngle;
        
        [Header("Setpoints")]
        [SerializeField] private ChillOutSetpoint stow;
        [SerializeField] private ChillOutSetpoint stowAlgae;
        [SerializeField] private ChillOutSetpoint intakeOut;
        [SerializeField] private ChillOutSetpoint coralTransferring;
        
        [SerializeField] private ChillOutSetpoint l4Front;
        [SerializeField] private ChillOutSetpoint l4Back;
        [SerializeField] private ChillOutSetpoint l3Front;
        [SerializeField] private ChillOutSetpoint l3Back;
        [SerializeField] private ChillOutSetpoint l2Front;
        [SerializeField] private ChillOutSetpoint l2Back;
        
        [SerializeField] private ChillOutSetpoint l1;
        
        [SerializeField] private ChillOutSetpoint groundAlgae;
        [SerializeField] private ChillOutSetpoint lolli;
        [SerializeField] private ChillOutSetpoint lowFront;
        [SerializeField] private ChillOutSetpoint lowBack;
        [SerializeField] private ChillOutSetpoint highFront;
        [SerializeField] private ChillOutSetpoint highBack;
        [SerializeField] private ChillOutSetpoint process;
        [SerializeField] private ChillOutSetpoint barge;
        
        [Header("Intake and Stow States")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake armCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState algaeStowState;
        
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
        
        [Header("Align Offsets")]
        [SerializeField] private AutoAlignOffset frontLeft;
        [SerializeField] private AutoAlignOffset frontRight;
        [SerializeField] private AutoAlignOffset backLeft;
        [SerializeField] private AutoAlignOffset backRight;
        
        [Header("Roller Stuff")]
        [SerializeField] private GenericRoller[] intakeRollers;
        private bool intaking;
        
        private ReefscapeAutoAlign align;

        private bool transferOnce = false;
        private bool intk = false;
        private bool transferring = false;
        private bool coralInPossesion = false;

        private ReefscapeSetpoints nextLevel = ReefscapeSetpoints.Stow;
        
        protected override void Start()
        {
            base.Start();
            
            align = gameObject.GetComponent<ReefscapeAutoAlign>();
            
            arm.SetPid(armPid);
            intake.SetPid(intakePid);

            _elevatorTargetHeight = 0;
            _armTargetAngle = 0;
            _intakeTargetAngle = 0;
            
            RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                coralIntakeState,
                coralStowState
            };
            _coralController.intakes.Add(armCoralIntake);
            _coralController.intakes.Add(coralIntake);
            
            _algaeController.gamePieceStates = new[]
            {
                algaeStowState
            };
            _algaeController.intakes.Add(algaeIntake);
        }

        private bool atSetpoint(ChillOutSetpoint stp)
        {
            return
                Utils.InAngularRange(elevator.GetElevatorHeight(), stp.elevatorHeight, 2f) &&
                Utils.InAngularRange(arm.GetSingleAxisAngle(JointAxis.X), stp.armAngle, 2f) &&
                Utils.InRange(intake.GetSingleAxisAngle(JointAxis.X), stp.intakeAngle, 2f);
        }
        
        public bool atSetpoint(ChillOutSetpoint stp, GenericJoint jnt)
        {
            return Utils.InAngularRange(jnt.GetSingleAxisAngle(JointAxis.X), stp.elevatorHeight, 2f);
        }
        
        public bool atSetpoint(ChillOutSetpoint stp, GenericElevator elv)
                {
                    return Utils.InRange(elv.GetElevatorHeight(), stp.elevatorHeight, 2f);
                }


        private void setIntakeIntake()
        {
            intakeRollers[0].SetAngularVelocity(-1000);
            intakeRollers[1].SetAngularVelocity(5000);
        }

        private void setIntakeOuttaking()
        {
            for (int i = 0; i < intakeRollers.Length; i++)
            {
                intakeRollers[i].SetAngularVelocity((i * -1) * 1500);
            }
        }
        
        private void setIntakeOuttaking(float s)
        {
            for (int i = 0; i < intakeRollers.Length; i++)
            {
                intakeRollers[i].SetAngularVelocity((i * -1) * s);
            }
        }

        private void FixedUpdate()
        {
            
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();
            bool intakeHasCoral = _coralController.atTarget && _coralController.currentStateNum == coralIntakeState.stateNum;
            bool armHasCoral = _coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum;
            
            _algaeController.SetTargetState(algaeStowState);
            
            if (!IntakeAction.IsPressed())
            {
                _algaeController.RequestIntake(algaeIntake, false);
                _coralController.RequestIntake(coralIntake, false);
                transferOnce = false;
            }

            if (intakeHasCoral || transferring)
            {
                coralInPossesion = true;
                setIntakeOuttaking();
            }

            if (armHasCoral)
            {
                _coralController.RequestIntake(armCoralIntake, false);
            }

            if (atSetpoint(l1))
            {
                setIntakeOuttaking(3000);
            }

            if (CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                if (intakeHasCoral && atSetpoint(stow))
                {
                    transferring = true;
                } 
                else if (armHasCoral && transferring)
                {
                    transferring = false;
                    SetState(nextLevel);
                }
            }

            if (atSetpoint(coralTransferring) && !transferOnce && CurrentIntakeMode == ReefscapeIntakeMode.Normal && CurrentSetpoint != ReefscapeSetpoints.L1)
            {
                transferToArm();
                transferOnce = true;
                intk = false;
            }
            else
            {
                setIntakeIntake();
            }

            if (CurrentIntakeMode == ReefscapeIntakeMode.Normal && intk)
            {
                _coralController.SetTargetState(coralIntakeState);
            }
            else if (CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                _coralController.SetTargetState(coralStowState);
            }
            else if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
            {
                _coralController.SetTargetState(coralIntakeState);
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (CurrentIntakeMode == ReefscapeIntakeMode.Normal && (intakeHasCoral || coralInPossesion) && transferring)
                    {
                        SetSetpoint(coralTransferring);
                    }
                    else
                    {
                        SetSetpoint(hasAlgae ? stowAlgae : stow);
                    }

                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, !transferring && atSetpoint(stow));
                    break;
                case ReefscapeSetpoints.Intake:
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral ||
                        !hasAlgae && !hasCoral)
                    {
                        SetSetpoint(intakeOut);
                    }

                    if (CurrentRobotMode == ReefscapeRobotMode.Algae && !armHasCoral)
                    {
                        SetSetpoint(groundAlgae);
                    }
                    
                    _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && !armHasCoral && IntakeAction.IsPressed());
                    if (!hasCoral && !atSetpoint(coralTransferring) && !transferOnce)
                    {
                        _coralController.RequestIntake(coralIntake, !transferring);
                        intk = true;
                    }

                    break;
                case ReefscapeSetpoints.Place:
                    PlacePiece();
                    if (LastSetpoint == ReefscapeSetpoints.L4 || LastSetpoint == ReefscapeSetpoints.L3 || LastSetpoint == ReefscapeSetpoints.L2)
                    {
                        SetSetpointPlaced(GetSetpointByLevel());
                    }

                    nextLevel = ReefscapeSetpoints.Stow;
                    break;
                case ReefscapeSetpoints.L1:
                    if (intakeHasCoral)
                    {
                        SetSetpoint(l1);
                    }
                    else
                    {
                        if (armHasCoral)
                        {
                            _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1, 0));
                        }
                        else
                        {
                            setIntakeIntake();
                            _coralController.SetTargetState(coralIntakeState);
                            _coralController.RequestIntake(coralIntake, true);
                            _coralController.RequestIntake(armCoralIntake, false);
                        }
                    }
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(lolli);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L2:
                    if (armHasCoral)
                    {
                        SetSetpoint(FacingReef ? l2Front : l2Back);
                    }
                    else
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        nextLevel = ReefscapeSetpoints.L2;
                    }
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? lowFront : lowBack);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L3:
                    if (armHasCoral)
                    {
                        SetSetpoint(FacingReef ? l3Front : l3Back);
                    }
                    else
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        nextLevel = ReefscapeSetpoints.L3;
                    }
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(FacingReef ? highFront : highBack);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L4:
                    if (armHasCoral)
                    {
                        SetSetpoint(FacingReef ? l4Front : l4Back);
                    }
                    else
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        nextLevel = ReefscapeSetpoints.L4;
                    }
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(process);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetSetpoint(barge);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    break;
                case ReefscapeSetpoints.Climb:
                    break;
                case ReefscapeSetpoints.Climbed:
                    break;
            }
            
            AutoAlignnnn();
            ApplySetpoints();
        }

        private void transferToArm()
        {
            if (_coralController.currentStateNum == coralIntakeState.stateNum && _coralController.atTarget)
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, -3, 0));
                _coralController.SetTargetState(coralStowState);
            }

            _coralController.RequestIntake(armCoralIntake, transferring); //atSetpoint(coralTransferring));
        }
        
        private void AutoAlignnnn()
        {
            if (AutoAlignLeftAction.IsPressed() && FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(frontLeft);
            }
            else if (AutoAlignRightAction.IsPressed() && FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(frontRight);
            }
            else if (AutoAlignLeftAction.IsPressed() && !FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(backLeft);
            }
            else if (AutoAlignRightAction.IsPressed() && !FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(backRight);
            }
        }

        private void SetAlignOffsets(AutoAlignOffset alignment)
        {
            align.offset = new Vector3(alignment.xOffset, alignment.yOffset, alignment.zOffset);
            align.rotation = alignment.Rotation;
        }

        private int GetLevelByState()
        {
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.L1:
                    return 1;
                case ReefscapeSetpoints.L2:
                    return 2;
                case ReefscapeSetpoints.L3:
                    return 3;
                case ReefscapeSetpoints.L4:
                    return 4;
            }
            
            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L1:
                    return 1;
                case ReefscapeSetpoints.L2:
                    return 2;
                case ReefscapeSetpoints.L3:
                    return 3;
                case ReefscapeSetpoints.L4:
                    return 4;
            }

            return 0;
        }

        private ChillOutSetpoint GetSetpointByLevel()
        {
            switch (GetLevelByState())
            {
                case 1:
                    return l1;
                case 2:
                    return FacingReef ? l2Front : l2Back;
                case 3:
                    return FacingReef ? l3Front : l3Back;
                case 4:
                    return FacingReef ? l4Front : l4Back;
            }

            return null;
        }

        private void PlacePiece()
        {
            if (_coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum)
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 1, 0));
                coralInPossesion = false;
            }
            else if (_coralController.atTarget && _coralController.currentStateNum == coralIntakeState.stateNum)
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, -5, 0));
                coralInPossesion = false;
            }
            else
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 4, 0));
            }
        }

        private void SetSetpointPlaced(ChillOutSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight - 5;
            _armTargetAngle = setpoint.armAngle - (FacingReef ? -7 : 7);
            _intakeTargetAngle = setpoint.intakeAngle;
        }

        private void SetRollerState()
        {
            
        }

        private void RunAudio()
        {
            
        }

        private void SetSetpoint(ChillOutSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _armTargetAngle = setpoint.armAngle;
            _intakeTargetAngle = setpoint.intakeAngle;
        }

        private bool isCurrentSetpoint(ChillOutSetpoint setpoint)
        {
            return
                _elevatorTargetHeight == setpoint.elevatorHeight &&
                _armTargetAngle == setpoint.armAngle &&
                _intakeTargetAngle == setpoint.intakeAngle;
        }

        
        private void ApplySetpoints() 
        {
            elevator.SetTarget(_elevatorTargetHeight);
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(
                ((CurrentRobotMode == ReefscapeRobotMode.Algae || CurrentSetpoint == ReefscapeSetpoints.HighAlgae ||
                  CurrentSetpoint == ReefscapeSetpoints.LowAlgae || LastSetpoint == ReefscapeSetpoints.HighAlgae ||
                  LastSetpoint == ReefscapeSetpoints.LowAlgae || CurrentSetpoint == ReefscapeSetpoints.Stack ||
                  LastSetpoint == ReefscapeSetpoints.Stack) || _algaeController.atTarget)
                    ? 180
                    : (FacingReef ? 150 : 210));
            intake.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X);
        }

        private void HandoffToArm()
        {
            _intakeTargetAngle = coralTransferring.intakeAngle;
            _armTargetAngle = coralTransferring.armAngle;
            if (Utils.InRange(_intakeTargetAngle, intake.GetSingleAxisAngle(JointAxis.X), 1f))
            {
                _elevatorTargetHeight = coralTransferring.elevatorHeight;
            }

        }
    }
}