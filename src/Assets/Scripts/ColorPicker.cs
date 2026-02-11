using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ChangeColor : MonoBehaviour
{
	[Header("Input Actions")]
	public InputActionReference paletteHoldAction; // left trigger

	[Header("Object References")]
	public Transform rightHandTransform;
	public Transform leftHandTransform;
	public GameObject paletteRoot; // disabled by default
	public Draw draw;

	[Header("Palette Placement")]
	public Vector3 localOffset = new Vector3(0.0f, 0.0f, 0.15f);
	public bool faceHandForward = true;

	[Header("Raycast")]
	public float rayDistance = 2.0f;
	public LayerMask swatchLayerMask = ~0;

	private Renderer hoveredRenderer;
	private Vector3 hoveredOriginalScale;
	private bool lastHolding;
	private Transform originalParent;

	// init deac palette
	private void OnEnable()
	{
		if (paletteHoldAction != null) paletteHoldAction.action.Enable();
	}

	private void OnDisable()
	{
		if (paletteHoldAction != null) paletteHoldAction.action.Disable();
	}

	private void Update()
	{
		bool holding = ReadAsPressed(paletteHoldAction);

		// show palette on trigger btn hold
		if (holding)
		{
			ShowPalette();
			UpdateHover();
		}

		// release edge -> set colour
		if (!holding && lastHolding)
		{
			CommitHoverAndHide();
		}

		lastHolding = holding;
	}

	private bool ReadAsPressed(InputActionReference actionRef)
	{
		if (actionRef == null || actionRef.action == null) return false;

		InputAction action = actionRef.action;

		// get trigger value
		if (action.activeControl is AxisControl)
		{
			return action.ReadValue<float>() > 0.1f;
		}

		return action.IsPressed();
	}

	private void ShowPalette()
	{
		if (paletteRoot == null || leftHandTransform == null) return;

		if (!paletteRoot.activeSelf)
		{
			originalParent = paletteRoot.transform.parent;

			paletteRoot.transform.SetParent(leftHandTransform, false);
			paletteRoot.transform.localPosition = localOffset;
			paletteRoot.transform.localRotation = Quaternion.identity;

			paletteRoot.SetActive(true);
		}
	}

	// get colour block that's being pointed at
	private void UpdateHover()
	{
		if (rightHandTransform == null || paletteRoot == null) return;

		Ray ray = new Ray(rightHandTransform.position, rightHandTransform.forward);

		if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, swatchLayerMask, QueryTriggerInteraction.Collide))
		{
			Renderer r = hit.collider.GetComponent<Renderer>();
			if (r != null && r.transform.IsChildOf(paletteRoot.transform))
			{
				SetHovered(r);
				return;
			}
		}

		ClearHovered();
	}

	// set color and clear hovered effect
	private void CommitHoverAndHide()
	{
		if (hoveredRenderer != null && draw != null)
		{
			draw.SetColor(hoveredRenderer.material.color);
		}

		ClearHovered();

		if (paletteRoot != null)
		{
			paletteRoot.SetActive(false);
			paletteRoot.transform.SetParent(originalParent, true);
		}
	}

	// make colour block bigger on hover
	private void SetHovered(Renderer r)
	{
		if (hoveredRenderer == r) return;

		ClearHovered();

		hoveredRenderer = r;
		hoveredOriginalScale = hoveredRenderer.transform.localScale;
		hoveredRenderer.transform.localScale = hoveredOriginalScale * 1.15f;
	}

	// clear hovered effect
	private void ClearHovered()
	{
		if (hoveredRenderer != null)
		{
			hoveredRenderer.transform.localScale = hoveredOriginalScale;
			hoveredRenderer = null;
		}
	}
}
