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

int samples = 64;
float blur_radius = 120.0;
float intensity = 20.0;
float sigma = 40.0;

float CalcGauss(float x, float sigma) {
    float coeff = 1.0 / (2.0 * 3.14157 * sigma);
    float expon = -(x * x) / (2.0 * sigma);
    return (coeff * exp(expon));
}
// does not seem to work right now :(
float get_bar_boost(float u_coord) {
    float x_px = u_coord * renderWidth;
    int index = int(x_px / (barWidth + barSpacing));
    if (index >= 0 && index < barCount) {
        float h = barHeights[index];
        return 1.0 + (h * 8.0);
    }
    return 1.0;
}

void main() {
    vec2 texC = fragTexCoord;
    vec2 texSize = vec2(renderWidth, renderHeight);
    vec2 onePixel = 1.0 / texSize;

    vec4 blur_sum = vec4(0.0);
    float total_weight = 0.0;
    
    float golden_angle = 2.39996323;

    for (int i = 0; i < samples; ++i) {
        float r_norm = sqrt(float(i) / float(samples)); 
        float theta = float(i) * golden_angle;
        
        float current_radius = r_norm * blur_radius;
        vec2 offset = vec2(cos(theta), sin(theta)) * current_radius * onePixel;
        vec2 sample_uv = clamp(texC + offset, vec2(0.0), vec2(1.0));
        vec4 sample_col = texture(texture0, sample_uv);
        float boost = get_bar_boost(sample_uv.x);
        float weight = CalcGauss(current_radius, sigma);
        
        blur_sum += sample_col * weight * boost;
        total_weight += weight;
    }

    vec4 blurred_color = blur_sum / total_weight;

    vec4 original = texture(texture0, texC);
    vec3 glow = blurred_color.rgb * intensity;
    
    float glow_lum = dot(glow, vec3(0.299, 0.587, 0.114));
    vec3 white_hot = vec3(1.0) * max(0.0, glow_lum - 0.8) * 0.5;
    vec3 final_rgb = original.rgb + glow + white_hot;

    finalColor = vec4(final_rgb * lightBrightness, 1.0);
}

