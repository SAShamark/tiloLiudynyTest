using UnityEngine;

namespace Gameplay.Effects
{
    public sealed class RendererTint
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock _block;

        public void SetBase(Renderer target, Color baseColor)
        {
            _block ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, baseColor);
            _block.SetColor(ColorId, baseColor);
            target.SetPropertyBlock(_block);
        }

        public void SetEmission(Renderer target, Color emission)
        {
            _block ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_block);
            _block.SetColor(EmissionId, emission);
            target.SetPropertyBlock(_block);
        }

        public void SetBaseAndEmission(Renderer target, Color baseColor, Color emission)
        {
            _block ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, baseColor);
            _block.SetColor(ColorId, baseColor);
            _block.SetColor(EmissionId, emission);
            target.SetPropertyBlock(_block);
        }
    }
}
