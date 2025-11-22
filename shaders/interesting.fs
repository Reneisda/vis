#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

out vec4 finalColor;

uniform sampler2D texture0;
uniform float renderWidth;
uniform float renderHeight;
uniform float lightBrightness;
uniform int barCount;
uniform float barHeights[128];
uniform float barWidth;
uniform float barSpacing;

float glow_radius = 4.0;
float glow_intensity = 2.0;
float decay = 0.90;

// i hate shadercode
void main() {
    vec2 uv = fragTexCoord;
    vec2 texelSize = vec2(1.0 / renderWidth, 1.0 / renderHeight);

    float blur_mask = 0.0;
    for (float i = -4.0; i <= 4.0; i++) {
        vec4 sample_col = texture(texture0, uv + vec2(i * glow_radius * texelSize.x, 0.0));
        
        float weight = 1.0 - abs(i) / 5.0;
        blur_mask += sample_col.r * weight;
    }
    blur_mask /= 3.0; 

    float x_coord_px = uv.x * renderWidth;
    int index = int(x_coord_px / (barWidth + barSpacing));
    
    float bar_power = 0.0;
    if (index >= 0 && index < barCount) {
        bar_power = barHeights[index];
    }
    
    vec3 glow_color = mix(vec3(0.0, 0.5, 1.0), vec3(1.0, 0.1, 0.1), bar_power);
    vec3 final_rgb = glow_color * blur_mask * glow_intensity;
    vec4 original_bar = texture(texture0, uv);
    final_rgb += original_bar.rgb; 
    finalColor = vec4(final_rgb * lightBrightness, 1.0);
}
