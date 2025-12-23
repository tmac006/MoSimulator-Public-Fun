using System.Collections;
using Codice.Client.Common.TreeGrouper;
using Games.Reefscape.Enums;
using Games.Reefscape.FieldScripts;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
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
        [Header("Robot Components")] [SerializeField]
        private GenericJoint wristJoint;

        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint climber;
        [SerializeField] private BoxCollider intakeCollider;
        [SerializeField] private GenericRoller[] funnelRollers;

        [Header("PID Constants")] [SerializeField]
        private PidConstants wristPIDMI;

        [SerializeField] private PidConstants climberPIDMI;

        [Header("Setpoints")] [SerializeField]
        private GRRSetpoint coralIntake;

        [SerializeField] private GRRSetpoint stow;
        [SerializeField] private GRRSetpoint coralStow;
        [SerializeField] private GRRSetpoint l1;
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

        [Header("Game Piece Intakes")] [SerializeField]
        private ReefscapeGamePieceIntake coralIntakeComponent;

        [Header("Game Piece States")] [SerializeField]
        private string currentState;

        [SerializeField] private GamePieceState stowState;

        [Header("Target Positions")] [SerializeField]
        private float targetWristAngle;

        [SerializeField] private float targetElevatorDistance;
        [SerializeField] private float targetClimberAngle;

        [Header("Robot Audio")] [SerializeField]
        private AudioSource rollerSource;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode coralController;

        private ReefscapeSetpoints _previousSetpoint;
        private bool alreadyPlaced;

        private GameObject targetReef;

        protected override void Start()
        {
            base.Start();

            wristJoint.SetPid(wristPIDMI);
            climber.SetPid(climberPIDMI);

            targetWristAngle = coralStow.wristTarget;
            targetElevatorDistance = 0;
            targetClimberAngle = stow.climberTarget;
            
            _previousSetpoint = ReefscapeSetpoints.Stow;

            RobotGamePieceController.SetPreload(stowState);
            coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());

            coralController.gamePieceStates = new[] { stowState };
            coralController.intakes.Add(coralIntakeComponent);

            alreadyPlaced = false;

            targetReef = Alliance == Alliance.Blue ? GameObject.Find("BlueReef") : GameObject.Find("RedReef");
        }

        private void LateUpdate()
        {
            wristJoint.UpdatePid(wristPIDMI);
            climber.UpdatePid(climberPIDMI);
        }

        private void FixedUpdate()
        {
            bool autoPlace = false;
            bool autoAligning = AutoAlignLeftAction.IsPressed() || AutoAlignRightAction.IsPressed();
            float reefDistance = (targetReef.transform.position - transform.position).magnitude;
            if (autoAligning && FacingReef && reefDistance <= 1.37f)
            {
                SetState(ReefscapeSetpoints.Place);
                autoPlace = true;
            }

            if (coralController.HasPiece())
            {
                foreach (var roller in funnelRollers)
                {
                    roller.flipVelocity();
                }
            }

            var readState = coralController.GetCurrentState();
            if (readState != null)
            {
                currentState = readState.name;
            }

            if (CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                alreadyPlaced = false;
            }

            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                return;
            }

            if (CurrentSetpoint == ReefscapeSetpoints.Intake)
            {
                intakeCollider.enabled = true;
            }
            else
            {
                intakeCollider.enabled = false;
            }

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    bool hasCoral = coralController.HasPiece();

                    if (hasCoral)
                    {
                        SetSetpoint(coralStow);
                    }
                    else
                    {
                        SetSetpoint(stow);
                    }

                    coralController.SetTargetState(stowState);
                    break;
                case ReefscapeSetpoints.Intake:
                    bool isCoral = CurrentRobotMode == ReefscapeRobotMode.Coral;

                    coralController.SetTargetState(stowState);
                    coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed() && isCoral);
                    break;
                case ReefscapeSetpoints.Place:
                    if (!alreadyPlaced && (OuttakeAction.triggered || autoPlace))
                    {
                        StartCoroutine(PlacePiece());
                    }
                    alreadyPlaced = true;
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                    break;
                case ReefscapeSetpoints.Stack:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.L2:
                    // If we don't have coral, redirect to algae setpoints
                    if (!coralController.HasPiece())
                    {
                        SetState(ReefscapeSetpoints.LowAlgae);
                    }
                    else
                    {
                        SetSetpoint(l2);
                        coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                    }
                    break;
                case ReefscapeSetpoints.L3:
                    // If we don't have coral, redirect to algae setpoints
                    if (!coralController.HasPiece())
                    {
                        SetState(ReefscapeSetpoints.HighAlgae);
                    }
                    else
                    {
                        SetSetpoint(l3);
                        coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                    }
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4);
                    coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(lowAlgae);
                    targetClimberAngle = lowAlgae.climberTarget;
                    if (coralController.HasPiece())
                    {
                        coralController.RequestIntake(coralIntakeComponent, false);
                    }
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(highAlgae);
                    targetClimberAngle = highAlgae.climberTarget;
                    if (coralController.HasPiece())
                    {
                        coralController.RequestIntake(coralIntakeComponent, false);
                    }
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climb);
                    targetClimberAngle = climb.climberTarget;
                    coralController.RequestIntake(coralIntakeComponent, false);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    targetClimberAngle = climbed.climberTarget;
                    coralController.SetTargetState(stowState);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            _previousSetpoint = CurrentSetpoint;
            UpdateSetpoints();
        }

        private void SetSetpoint(GRRSetpoint setpoint)
        {
            targetWristAngle = setpoint.wristTarget;
            targetElevatorDistance = setpoint.elevatorDistance;
            targetClimberAngle = setpoint.climberTarget;
        }

        private void UpdateSetpoints()
        {
            wristJoint.SetTargetAngle(targetWristAngle).withAxis(JointAxis.X).flipDirection();
            elevator.SetTarget(targetElevatorDistance);
            climber.SetTargetAngle(targetClimberAngle).withAxis(JointAxis.Z).flipDirection();
        }

        private IEnumerator PlacePiece()
        {
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

            //default mode is impulse
            if (CurrentRobotMode == ReefscapeRobotMode.Coral && coralController.HasPiece())
            {
                var time = 1.0f;
                // do not change this line
                // if you think what you're doing is going
                // to work, it won't. don't even fucking bother
                Vector3 force = new Vector3(0, -0.2f, 2.73f);
                var maxSpeed = 0.75f;

                if (LastSetpoint == ReefscapeSetpoints.L4 || LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    time = 1.5f;
                    force = new Vector3(0, 0.0f, 4f);
                    maxSpeed = 0.25f;
                }
                else if (LastSetpoint == ReefscapeSetpoints.L1)
                {
                    time = 0.15f;
                    force = new Vector3(0, 0, 0.25f);
                    maxSpeed = 0.15f;
                }

                coralController.ReleaseGamePieceWithContinuedForce(force, time, maxSpeed);
            }
            
            yield break;
        }
    }
}

