using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
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
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;
        [SerializeField] private GamePieceState algaeStowState;
        
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;
        
        protected override void Start()
        {
            base.Start();
            
            arm.SetPid(armPid);
            intake.SetPid(intakePid);

            _elevatorTargetHeight = 0;
            _armTargetAngle = 0;
            _intakeTargetAngle = 0;
            
            //RobotGamePieceController.SetPreload(coralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                coralIntakeState,
                coralStowState
            };
            _coralController.intakes.Add(coralIntake);
            
            _algaeController.gamePieceStates = new[]
            {
                algaeStowState
            };
            _algaeController.intakes.Add(algaeIntake);
        }

        private void FixedUpdate()
        {
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();
            bool intakeHasCoral = _coralController.atTarget && _coralController.currentStateNum == coralIntakeState.stateNum;
            bool armHasCoral = _coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum;
            
            _algaeController.SetTargetState(algaeStowState);
            _coralController.SetTargetState(coralStowState);
            
            if (!IntakeAction.IsPressed())
            {
                _algaeController.RequestIntake(algaeIntake, false);
                _coralController.RequestIntake(coralIntake, false);
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(hasAlgae ? stowAlgae : stow);
                    if (intakeHasCoral)
                    {
                        StartCoroutine(transferCoral());
                    }
                    
                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.Intake:
                    if (CurrentRobotMode == ReefscapeRobotMode.Coral || !hasAlgae)
                    {
                        SetSetpoint(intakeOut);
                    }
                    
                    _algaeController.RequestIntake(algaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && !hasCoral && IntakeAction.IsPressed());
                    _coralController.SetTargetState(coralIntakeState);
                    _coralController.RequestIntake(coralIntake, !hasCoral && !hasAlgae && IntakeAction.IsPressed());
                    break;
                case ReefscapeSetpoints.Place:
                    PlacePiece();
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(lolli);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(FacingReef ? l2Front : l2Back);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? lowFront : lowBack);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(FacingReef ? l3Front : l3Back);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(FacingReef ? highFront : highBack);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsInProgress() && !hasAlgae && !hasCoral);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(FacingReef ? l4Front : l4Back);
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
            
            ApplySetpoints();
        }
        
        public void SetCoralTargetStow() {
            _coralController.SetTargetState(coralStowState);
        }

        public int GetLevelByState()
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

            return 0;
        }

        public ChillOutSetpoint GetSetpointByLevel()
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

        public void PlacePiece()
        {
            if (_coralController.atTarget && _coralController.currentStateNum == coralStowState.stateNum)
            {
                //StartCoroutine(PlaceCoralOnBranch());
                _coralController.ReleaseGamePieceWithForce(new Vector3(1, 1, 1));
                _elevatorTargetHeight = GetSetpointByLevel().elevatorHeight - 3;
                _armTargetAngle = GetSetpointByLevel().armAngle - (FacingReef ? -5 : 5);
            }
            else
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(1, 1, 1));
            }
        }

        public IEnumerator PlaceCoralOnBranch()
        {
            yield return new WaitForSeconds(0.5f);
        }

        public void SetRollerState()
        {
            
        }

        public void RunAudio()
        {
            
        }

        public void SetSetpoint(ChillOutSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _armTargetAngle = setpoint.armAngle;
            _intakeTargetAngle = setpoint.intakeAngle;
        }
        
        public void ApplySetpoints() 
        {
            elevator.SetTarget(_elevatorTargetHeight);
            arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X).noWrap(_algaeController.atTarget ? 180 : -1.5f);
            intake.SetTargetAngle(_intakeTargetAngle);
        }

        public IEnumerator transferCoral()
        {
            yield return new WaitForSeconds(0.5f);
        }
    }
}