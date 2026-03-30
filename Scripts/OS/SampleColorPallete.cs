using UnityEngine;

using SPACE_UTIL;

public class SampleColorPallete : MonoBehaviour
{



	[SerializeField]
	Color
		bg_deepest,
		surface_panels_darkNavy,
		border,
		borderActive,
		textPrim,
		textSecon,
		textMuted,
		textDisabled,
		accentBank,
		accentAnalytics,
		accentStockMarket,
		accentCRM,
		positive,
		negative,
		warning;

	private void Awake()
	{
		Debug.Log(C.method(this));
		this.Init();
	}
	void Init()
	{
		this.bg_deepest = "#05080E".hex2Col();
		this.surface_panels_darkNavy = "0D1520".hex2Col();
		this.border = "FFFFFF11".hex2Col();
		this.borderActive = "FFFFFF22".hex2Col();
		this.textPrim = "F1F5F9".hex2Col();
		this.textSecon = "94A3B8".hex2Col();
		this.textMuted = "4B5563".hex2Col();
		this.textDisabled = "1F2937".hex2Col();

		this.accentBank = "1F2937".hex2Col();
		this.accentAnalytics = "22C55E".hex2Col();
		this.accentStockMarket = "60A5FA   ".hex2Col();
		this.accentCRM = "A78BFA   ".hex2Col();

		this.positive = "22C55E".hex2Col();
		this.negative = "EF4444".hex2Col();
		this.warning = "F59E0B".hex2Col();

	}
	/*
	@font-face {
		font-family: Anthropic Sans;
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/cc27851ad-CFxw3nG7.woff2) format("woff2");
		font-weight: 300 800;
		font-style: normal;
		font-display: swap;
		font-feature-settings: "dlig" 0
	}

	@font-face {
		font-family: Anthropic Sans;
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/c9d3a3a49-BI1hrwN4.woff2) format("woff2");
		font-weight: 300 800;
		font-style: italic;
		font-display: swap;
		font-feature-settings: "dlig" 0
	}

	@font-face {
		font-family: "Anthropic Serif";
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/c66fc489e-C-BHYa_K.woff2) format("woff2");
		font-weight: 300 800;
		font-style: normal;
		font-display: swap
	}

	@font-face {
		font-family: "Anthropic Serif";
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/cc410af59-DH94ugQz.woff2) format("woff2");
		font-weight: 300 800;
		font-style: italic;
		font-display: swap
	}

	@font-face {
		font-family: Anthropic Mono;
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/c5dbe0935-B88FVziN.woff2) format("woff2");
		font-weight: 400;
		font-style: normal;
		font-display: swap
	}

	@font-face {
		font-family: Anthropic Mono;
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/c2f08283e-DHGc3er-.woff2) format("woff2");
		font-weight: 400;
		font-style: italic;
		font-display: swap
	}

	@font-face {
		font-family: OpenDyslexic;
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/cf5d8819d-D7oQwCA_.woff2) format("woff2");
		font-weight: 400;
		font-style: normal;
		font-display: swap
	}

	@font-face {
		font-family: OpenDyslexic;
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/ce6cfa480-T9ivnrk-.woff2) format("woff2");
		font-weight: 700;
		font-style: normal;
		font-display: swap
	}

	@font-face {
		font-family: OpenDyslexic;
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/c59d6eb1d-lZojiKrH.woff2) format("woff2");
		font-weight: 400;
		font-style: italic;
		font-display: swap
	}

	@font-face {
		font-family: OpenDyslexic;
		src: url(https://assets-proxy.anthropic.com/claude-ai/v2/assets/v1/ce4f31d76-ChjKfT4W.woff2) format("woff2");
		font-weight: 700;
		font-style: italic;
		font-display: swap
	}

	:root {
		--font-anthropic-sans: "Anthropic Sans", system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
		--font-anthropic-serif: "Anthropic Serif", Georgia, "Times New Roman", serif;
		--font-anthropic-mono: "Anthropic Mono", ui-monospace, monospace;
		--font-open-dyslexic: "OpenDyslexic", "Comic Sans MS", ui-serif, serif
	}
	*/
}
