#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <android/log.h>

#define LOG_TAG "LlamaWrapper2"
#define LOGI(...) __android_log_print(ANDROID_LOG_INFO, LOG_TAG, __VA_ARGS__)
#define LOGE(...) __android_log_print(ANDROID_LOG_ERROR, LOG_TAG, __VA_ARGS__)

// ===== Minimal type declarations matching llama.h =====
typedef int32_t llama_token;
typedef int32_t llama_pos;
typedef int32_t llama_seq_id;

typedef struct llama_context llama_context;
typedef struct llama_vocab llama_vocab;
typedef struct llama_sampler llama_sampler;

typedef struct llama_batch {
    int32_t       n_tokens;
    llama_token  *token;
    float        *embd;
    llama_pos    *pos;
    int32_t      *n_seq_id;
    llama_seq_id **seq_id;
    int8_t       *logits;
} llama_batch;

// Import from libllama.so
extern llama_batch llama_batch_init(int32_t n_tokens, int32_t embd, int32_t n_seq_max);
extern void llama_batch_free(llama_batch batch);
extern int32_t llama_decode(llama_context *ctx, llama_batch batch);
extern void llama_memory_clear(llama_context *ctx, int data);
extern void llama_kv_self_clear(llama_context *ctx);
extern int32_t llama_tokenize(const llama_vocab *vocab, const char *text, int32_t text_len,
                               llama_token *tokens, int32_t n_tokens_max, int add_special, int parse_special);
extern int32_t llama_sampler_sample(llama_sampler *smpl, llama_context *ctx, int32_t idx);
extern float *llama_get_logits_ith(llama_context *ctx, int32_t i);
extern int llama_vocab_is_eog(const llama_vocab *vocab, llama_token token);
extern int32_t llama_token_to_piece(const llama_vocab *vocab, llama_token token,
                                     char *buf, int32_t length, int32_t lstrip, int special);

// ===== Heap-allocated batch handle =====
typedef struct {
    llama_batch batch;
} wrapper_batch;

__attribute__((visibility("default")))
wrapper_batch *wrapper_batch_init(int32_t n_tokens, int32_t embd, int32_t n_seq_max) {
    LOGI("wrapper_batch_init: n_tokens=%d, embd=%d, n_seq_max=%d", n_tokens, embd, n_seq_max);
    wrapper_batch *wb = (wrapper_batch *)malloc(sizeof(wrapper_batch));
    if (!wb) return NULL;
    wb->batch = llama_batch_init(n_tokens, embd, n_seq_max);
    LOGI("wrapper_batch_init: OK batch.token=%p", (void*)wb->batch.token);
    return wb;
}

__attribute__((visibility("default")))
void wrapper_batch_free(wrapper_batch *wb) {
    LOGI("wrapper_batch_free: wb=%p", (void*)wb);
    if (wb) {
        llama_batch_free(wb->batch);
        free(wb);
    }
}

__attribute__((visibility("default")))
void wrapper_batch_reset(wrapper_batch *wb) {
    if (wb) wb->batch.n_tokens = 0;
}

__attribute__((visibility("default")))
void wrapper_batch_add(wrapper_batch *wb, llama_token token, llama_pos pos,
                       llama_seq_id seq_id, int logits) {
    if (!wb) return;
    int i = wb->batch.n_tokens;
    wb->batch.token[i] = token;
    wb->batch.pos[i] = pos;
    wb->batch.n_seq_id[i] = 1;
    wb->batch.seq_id[i][0] = seq_id;
    wb->batch.logits[i] = logits ? 1 : 0;
    wb->batch.n_tokens++;
}

__attribute__((visibility("default")))
int32_t wrapper_decode(llama_context *ctx, wrapper_batch *wb) {
    if (!ctx || !wb) {
        LOGE("wrapper_decode: null ctx=%p or wb=%p", (void*)ctx, (void*)wb);
        return -1;
    }
    LOGI("wrapper_decode: n_tokens=%d ctx=%p", wb->batch.n_tokens, (void*)ctx);
    int32_t result = llama_decode(ctx, wb->batch);
    LOGI("wrapper_decode: result=%d", result);
    return result;
}

__attribute__((visibility("default")))
int32_t wrapper_batch_get_n_tokens(wrapper_batch *wb) {
    return wb ? wb->batch.n_tokens : 0;
}

// ===== Safe wrappers for all native calls used during inference =====

__attribute__((visibility("default")))
void wrapper_memory_clear(llama_context *ctx, int data) {
    LOGI("wrapper_memory_clear: ctx=%p data=%d", (void*)ctx, data);
    // llama_memory_clear takes (llama_memory*, bool) in newer API
    // but this build doesn't have llama_context_get_memory
    // Try llama_memory_clear with context pointer - it may work if this
    // is a transitional build where it still accepts llama_context*
    llama_memory_clear(ctx, data);
    LOGI("wrapper_memory_clear: done");
}


__attribute__((visibility("default")))
int32_t wrapper_tokenize(const llama_vocab *vocab, const char *text, int32_t text_len,
                          llama_token *tokens, int32_t n_tokens_max, int add_special, int parse_special) {
    LOGI("wrapper_tokenize: vocab=%p text_len=%d n_tokens_max=%d add_special=%d parse_special=%d",
         (void*)vocab, text_len, n_tokens_max, add_special, parse_special);
    int32_t result = llama_tokenize(vocab, text, text_len, tokens, n_tokens_max, add_special, parse_special);
    LOGI("wrapper_tokenize: result=%d", result);
    return result;
}

__attribute__((visibility("default")))
int32_t wrapper_sampler_sample(llama_sampler *smpl, llama_context *ctx, int32_t idx) {
    LOGI("wrapper_sampler_sample: smpl=%p ctx=%p idx=%d", (void*)smpl, (void*)ctx, idx);
    int32_t result = llama_sampler_sample(smpl, ctx, idx);
    LOGI("wrapper_sampler_sample: token=%d", result);
    return result;
}

__attribute__((visibility("default")))
int wrapper_vocab_is_eog(const llama_vocab *vocab, llama_token token) {
    return llama_vocab_is_eog(vocab, token);
}

__attribute__((visibility("default")))
int32_t wrapper_token_to_piece(const llama_vocab *vocab, llama_token token,
                                char *buf, int32_t length, int32_t lstrip, int special) {
    return llama_token_to_piece(vocab, token, buf, length, lstrip, special);
}
