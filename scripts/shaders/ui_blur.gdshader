shader_type canvas_item;

// Godot 4.4+: SCREEN_TEXTURE is removed. Use a uniform with hint_screen_texture.
uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;

// Blur strength. Larger = blurrier.
uniform float blur_amount : hint_range(0.0, 6.0) = 2.5;

// Optional: darken the blurred background a bit (0 = no darken, 1 = fully black).
uniform float dim_amount : hint_range(0.0, 1.0) = 0.15;

void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 px = SCREEN_PIXEL_SIZE * blur_amount;

	// Multi-tap blur using SCREEN_TEXTURE.
	vec4 c = vec4(0.0);
	c += texture(screen_tex, uv) * 0.20;
	c += texture(screen_tex, uv + vec2(px.x, 0.0)) * 0.15;
	c += texture(screen_tex, uv - vec2(px.x, 0.0)) * 0.15;
	c += texture(screen_tex, uv + vec2(0.0, px.y)) * 0.15;
	c += texture(screen_tex, uv - vec2(0.0, px.y)) * 0.15;
	c += texture(screen_tex, uv + px) * 0.10;
	c += texture(screen_tex, uv - px) * 0.10;

	// Slight dim to help the popup stand out.
	c.rgb = mix(c.rgb, vec3(0.0), dim_amount);

	COLOR = c;
}
