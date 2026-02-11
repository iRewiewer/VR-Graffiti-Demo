using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Draw : MonoBehaviour
{
	[Header("Input Actions")]
	public InputActionReference drawAction; // right trigger btn
	public InputActionReference sizeUpAction; // right B btn
	public InputActionReference sizeDownAction; // right A btn

	[Header("Objecct References")]
	public Transform penTip;
	public Renderer colorPreviewRenderer;

	[Header("Stroke Settings")]
	public Material strokeMaterial;
	public int size = 3;
	public float minWidth = 0.0025f;
	public float maxWidth = 0.02f;
	public float minVertexDistance = 0.0005f;

	[Header("Size Popup")]
	public GameObject sizePopupRoot;
	public TMPro.TextMeshProUGUI sizePopupText;
	public float popupDuration = 1.0f;

	[Header("Wall Raycast Drawing")]
	public float wallRayDistance = 2.0f;
	public LayerMask wallLayerMask = 0;
	public float surfaceOffset = 0.002f; // 2mm off the wall to avoid z-fighting

	[Header("State")]
	public Color currentColor = Color.red;

	private float popupTimer;
	private bool wasDrawing;
	private TrailRenderer currentTrail;

	public float surfaceFollowLerp = 30f;
	private Vector3 smoothedPos;
	private bool hasSmoothedPos;
	private int strokeIndex = 0;
	public float strokeLayerStep = 0.0002f; // 0.2mm
	private float currentStrokeDepth;

	// init/deac actions
	private void OnEnable()
	{
		if (drawAction != null) drawAction.action.Enable();
		if (sizeUpAction != null) sizeUpAction.action.Enable();
		if (sizeDownAction != null) sizeDownAction.action.Enable();
	}

	private void OnDisable()
	{
		if (drawAction != null) drawAction.action.Disable();
		if (sizeUpAction != null) sizeUpAction.action.Disable();
		if (sizeDownAction != null) sizeDownAction.action.Disable();
	}

	private void Update()
	{
		// set size
		if (sizeUpAction != null && sizeUpAction.action.WasPressedThisFrame())
		{
			size = Mathf.Clamp(size + 1, 1, 25);
			ShowSizePopup();

		}

		if (sizeDownAction != null && sizeDownAction.action.WasPressedThisFrame())
		{
			size = Mathf.Clamp(size - 1, 1, 25);
			ShowSizePopup();
		}

		if (currentTrail != null)
		{
			float width = Mathf.Lerp(minWidth, maxWidth, (size - 1) / 9.0f);
			currentTrail.startWidth = width;
			currentTrail.endWidth = width;
		}

		bool triggerHeld = ReadAsPressed(drawAction);
		bool hasWall = TryGetWallHit(out RaycastHit hit);

		bool shouldDraw = triggerHeld && hasWall;

		if (shouldDraw && !wasDrawing)
		{
			StartNewStrokeAt(hit);
			hasSmoothedPos = false; // reset smoothing for new stroke
		}

		if (!shouldDraw && wasDrawing)
		{
			StopCurrentStroke();
		}

		if (shouldDraw && currentTrail != null)
		{
			Vector3 target = hit.point + hit.normal * currentStrokeDepth;

			if (!hasSmoothedPos)
			{
				smoothedPos = target;
				hasSmoothedPos = true;
			}
			else
			{
				smoothedPos = Vector3.Lerp(smoothedPos, target, Time.deltaTime * surfaceFollowLerp);
			}

			currentTrail.transform.position = smoothedPos;
		}

		wasDrawing = shouldDraw;

		if (sizePopupRoot != null && sizePopupRoot.activeSelf)
		{
			popupTimer -= Time.deltaTime;
			if (popupTimer <= 0f)
			{
				sizePopupRoot.SetActive(false);
			}
		}
	}

	private void ShowSizePopup()
	{
		Debug.Log($"SHOW SIZE POPUP: {size}");

		if (sizePopupRoot == null || sizePopupText == null) return;

		sizePopupText.text = $"Size: {size}";
		sizePopupRoot.SetActive(true);
		popupTimer = popupDuration;
	}

	private void StopCurrentStroke()
	{
		if (currentTrail == null) return;

		currentTrail.emitting = false;

		// detach
		currentTrail.transform.SetParent(null, true);

		currentTrail = null;
		hasSmoothedPos = false;
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

	private void StartNewStrokeAt(RaycastHit hit)
	{
		strokeIndex++;
		currentStrokeDepth = surfaceOffset + strokeIndex * strokeLayerStep;

		// set stroke
		GameObject stroke = new GameObject("Stroke");
		stroke.transform.position = hit.point + hit.normal * currentStrokeDepth;
		//stroke.transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);

		// set trail
		TrailRenderer tr = stroke.AddComponent<TrailRenderer>();
		tr.time = Mathf.Infinity;
		tr.minVertexDistance = minVertexDistance;
		tr.emitting = true;
		tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		tr.receiveShadows = false;

		tr.numCornerVertices = 6;
		tr.numCapVertices = 6;

		float width = Mathf.Lerp(minWidth, maxWidth, (size - 1) / 9.0f);
		tr.startWidth = width;
		tr.endWidth = width;

		tr.alignment = LineAlignment.TransformZ; // stroke aligns to the wall object's Z

		if (strokeMaterial != null)
		{
			tr.material = new Material(strokeMaterial); // unlit mat so we can change its colour
		}
		else
		{
			tr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
		}

		tr.material.SetColor("_BaseColor", currentColor);

		currentTrail = tr;
	}

	public void SetColor(Color c)
	{
		currentColor = c;

		if (colorPreviewRenderer != null)
		{
			colorPreviewRenderer.material.SetColor("_BaseColor", currentColor);
		}
	}

	// for drawing on wall
	// obj must have "wall" tag
	private bool TryGetWallHit(out RaycastHit hit)
	{
		hit = default;

		if (penTip == null) return false;
		
		float radius = 0.01f;
		Vector3 origin = penTip.position;
		Vector3 dir = penTip.forward;

		if (Physics.SphereCast(origin, radius, dir, out hit, wallRayDistance, wallLayerMask, QueryTriggerInteraction.Ignore))
		{
			return hit.collider.CompareTag("wall");
		}

		return false;
	}

}
