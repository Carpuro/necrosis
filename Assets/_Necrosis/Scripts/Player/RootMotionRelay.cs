using UnityEngine;

/// <summary>
/// NECROSIS://PROTOCOLO — Relé de root motion.
/// El modelo rigged es HIJO de la cápsula; su Animator recibe el root motion, pero
/// el movimiento/colisión están en la cápsula padre. Este componente vive en el
/// modelo, captura el root motion del clip en OnAnimatorMove y lo EXPONE para que
/// PlayerController lo aplique al padre cuando toca (enfoque híbrido recomendado:
/// locomoción recta por código, GIROS por root motion → sin deslizamiento de pies).
///
/// Con applyRootMotion=true y este OnAnimatorMove definido, Unity NO aplica el root
/// motion solo: nosotros decidimos cuándo usarlo (solo en giros).
/// </summary>
[RequireComponent(typeof(Animator))]
public class RootMotionRelay : MonoBehaviour
{
    Animator anim;

    /// <summary>Rotación del root este frame (para girar el cuerpo con la animación).</summary>
    public Quaternion DeltaRotation { get; private set; } = Quaternion.identity;
    /// <summary>Desplazamiento del root este frame (por si se quiere usar en giros).</summary>
    public Vector3 DeltaPosition { get; private set; }

    void Awake() => anim = GetComponent<Animator>();

    void OnAnimatorMove()
    {
        if (anim == null) return;
        DeltaRotation = anim.deltaRotation;
        DeltaPosition = anim.deltaPosition;
    }
}
