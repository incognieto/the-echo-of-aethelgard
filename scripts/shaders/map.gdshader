shader_type spatial;

// Menggunakan render_mode yang sama agar shading identik
render_mode depth_draw_always, cull_disabled, diffuse_toon, specular_toon;

group_uniforms Data_Tekstur;
uniform sampler2D tekstur_dinding : source_color;
group_uniforms;

// KONTROL TOON (Bayangan)
uniform vec4 shadow_color : source_color = vec4(0.2, 0.2, 0.2, 1.0);
uniform float shadow_threshold : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    // Cuma render tekstur biasa tanpa logika discard/lingkaran
    vec4 tex = texture(tekstur_dinding, UV);
    ALBEDO = tex.rgb;
}
