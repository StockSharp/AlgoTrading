# Estratégia Bezier de Desvio Padrão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia pontos de viragem na volatilidade usando um indicador de desvio padrão. Interpreta os mínimos e máximos locais do indicador como potenciais reversões na ação do preço. Quando o desvio padrão forma um vale, o sistema espera que a volatilidade se expanda para cima e abre uma posição comprada. Quando aparece um pico, vende a descoberto antecipando uma contração da volatilidade.

A abordagem é projetada para operações tanto compradas quanto vendidas num período de quatro horas por padrão. Não aplica ordens de stop-loss, focando-se em saídas baseadas em sinais.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O valor do desvio padrão na barra anterior é inferior aos seus vizinhos (mínimo local).
  - **Vendido**: O valor do desvio padrão na barra anterior é superior aos seus vizinhos (máximo local).
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Um sinal oposto desencadeia uma reversão.
- **Stops**: Não.
- **Valores padrão**:
  - `StdDev Period` = 9.
  - `Candle Type` = velas de 4 horas.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Desvio padrão
  - Stops: Não
  - Complexidade: Simples
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
