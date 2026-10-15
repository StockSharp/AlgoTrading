# Estratégia MaDelta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia MaDelta mede a diferença entre uma média móvel rápida e uma lenta. A diferença é escalada por um multiplicador e elevada à terceira potência, produzindo um valor oscilante `px`. Dois limiares dinâmicos separados por `Delta` (em pips) rastreiam a máxima e mínima recente deste valor. Quando `px` rompe acima do limiar superior, a estratégia muda para viés comprado; quando `px` cai abaixo do limiar inferior, muda para viés vendido. Posições existentes opostas ao novo viés são fechadas e uma nova operação é aberta na direção do sinal.

A abordagem captura efetivamente explosões de momentum quando a distância entre as duas médias móveis se expande rapidamente. Elevar ao cubo a diferença exagera movimentos fortes enquanto filtra pequenas flutuações. O parâmetro `Delta` define até onde `px` deve percorrer antes de uma reversão ser reconhecida, evitando sinais falsos em mercados laterais.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `px > hi` define `trade = 1` e abre uma posição comprada quando não há posição.
  - **Vendido**: `px < lo` define `trade = -1` e abre uma posição vendida quando está neutro.
- **Lógica de reversão**:
  - Sinal comprado enquanto vendido fecha o vendido com uma compra a mercado antes de entrar comprado.
  - Sinal vendido enquanto comprado fecha o comprado com uma venda a mercado antes de entrar vendido.
- **Indicadores**:
  - Média móvel rápida (SMA) com período `FastMaPeriod`.
  - Média móvel lenta (EMA) com período `SlowMaPeriod`.
  - Oscilador: `px = ((Multiplier * 0.1) * (FastMA - SlowMA))^3`.
- **Parâmetros**:
  - `Delta` – tamanho do canal alto/baixo em pips.
  - `Multiplier` – escala a diferença de MA antes de elevar ao cubo.
  - `FastMaPeriod` – comprimento da SMA rápida.
  - `SlowMaPeriod` – comprimento da EMA lenta.
  - `Volume` – volume da ordem nas entradas.
  - `CandleType` – período das velas processadas.
- **Outras notas**:
  - Funciona apenas com velas completadas.
  - Sem stop-loss ou take-profit explícitos; as posições se revertem em sinais opostos.
  - Usa a API de alto nível com vinculação de indicadores e gráficos automáticos.
