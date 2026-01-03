using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework.Controllers.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.GRR._340
{
    public class GRRLights : MonoBehaviour
    {
        [Header("LED Strips")]
        public GameObject leftStrip;
        public GameObject rightStrip;
        public GameObject topStrip;

        [Header("Textures")]
        public Shader shaderGraphShader;
        public Texture disabledBlue;
        public Texture disabledRed;
        public Texture climbing;
        public Texture autoAligning;
        public Texture hasCoral;

        private ReefscapeRobotBase _base;
        private GRRAutoAlign _autoAlign;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        
        private Material _leftMaterial;
        private Material _rightMaterial;
        private Material _topMaterial;

        private void Start()
        {
            _base = gameObject.GetComponent<ReefscapeRobotBase>();
            _autoAlign = gameObject.GetComponent<GRRAutoAlign>();
            _coralController = gameObject
                .GetComponent<RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>>()
                ?.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());

            _leftMaterial = new Material(shaderGraphShader);
            leftStrip.GetComponent<Renderer>().material = _leftMaterial;

            _rightMaterial = new Material(shaderGraphShader);
            rightStrip.GetComponent<Renderer>().material = _rightMaterial;

            _topMaterial = new Material(shaderGraphShader);
            topStrip.GetComponent<Renderer>().material = _topMaterial;
        }
        
        private void Update()
        {
            if (_base == null || _coralController == null) return;

            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                SetSides(_base.Alliance == Alliance.Blue ? disabledBlue : disabledRed, 20f);
                SetTop(null, 0f);
            }
            else if (_base.CurrentSetpoint == ReefscapeSetpoints.Climb)
            {
                SetSides(climbing, 20f);
                SetTop(null, 0f);
            }
            else if (_base.CurrentSetpoint == ReefscapeSetpoints.Climbed)
            {
                SetAll(climbing, Time.time % 0.18 > 0.09  ? 20f : 0f);
            }
            else
            {
                float blink = Time.time % 0.5 > 0.25 ? 20f : 0f;

                if (_base.AutoAlignLeftAction.IsPressed() || _base.AutoAlignRightAction.IsPressed())
                {
                    Set(_leftMaterial, autoAligning, _autoAlign.Left() ? blink : 20f);
                    Set(_rightMaterial, autoAligning, !_autoAlign.Left() ? blink : 20f);
                }
                else
                {
                    SetSides(null, 0f);
                }

                if (_coralController.HasPiece() && _coralController.atTarget)
                {
                    SetTop(hasCoral, blink);
                }
                else
                {
                    SetTop(null, 0f);
                }
            }
        }

        private void SetAll(Texture texture, float intensity)
        {
            SetSides(texture, intensity);
            SetTop(texture, intensity);
        }

        private void SetSides(Texture texture, float intensity)
        {
            Set(_leftMaterial, texture, intensity);
            Set(_rightMaterial, texture, intensity);
        }

        private void SetTop(Texture texture, float intensity)
        {
            Set(_topMaterial, texture, intensity);
        }

        private void Set(Material material, Texture texture, float intensity)
        {
            material.SetFloat("_X", 0f);
            material.SetFloat("_Y", 0f);
            material.SetFloat("_intensity", intensity);

            if (texture != null)
            {
                material.SetTexture("_Texture2D", texture);
            }
        }
    }
}