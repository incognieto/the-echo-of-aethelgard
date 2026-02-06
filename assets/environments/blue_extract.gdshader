shader_type spatial;
// render_mode ini fokus buat nembus objek transparan (botol) tapi tetep tunduk sama objek padat (tutup)
render_mode blend_mix, depth_draw_always, cull_disabled;

uniform vec4 water_color : source_color = vec4(0.0, 0.4, 0.9, 0.5);

void fragment() {
    ALBEDO = water_color.rgb;
    ALPHA = water_color.a;
    ROUGHNESS = 0.1;
    SPECULAR = 0.5;
}