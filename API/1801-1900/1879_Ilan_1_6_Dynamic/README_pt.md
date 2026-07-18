# Estratégia de Grade Dinâmica Ilan 1.6
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Ilan 1.6 Dynamic é um consultor especialista clássico de grade e martingale. Ela abre uma operação inicial em uma direção selecionada e coloca ordens adicionais cada vez que o preço se move contra a posição por um passo fixo. O volume de novas ordens cresce geometricamente por um expoente de lote. Todas as posições na cesta são fechadas quando o preço retorna ao preço de entrada médio mais uma distância de take profit. Um stop trailing pode opcionalmente proteger os lucros se o preço se mover suficientemente na direção favorável.

O algoritmo depende apenas do movimento do preço e não utiliza indicadores. Como o tamanho da posição aumenta após cada movimento adverso, o sistema carrega alto risco, mas pode capturar reversões rápidas.

## Detalhes

- **Entrada**
  - A primeira ordem é aberta na direção configurada.
  - Ordens adicionais são adicionadas a cada `PipStep` pontos contra a posição atual, até `MaxTrades`.
  - Volume de cada nova ordem = `InitialVolume * LotExponent^N`.
- **Saída**
  - Fechar tudo quando o preço tocar `AveragePrice ± TakeProfit`.
  - Stop trailing opcional começa após `TrailStart` pontos de lucro e segue o preço à distância `TrailStop`.
- **Gestão de posição**
  - Somente série comprada ou somente vendida por vez.
  - Após fechar a cesta, a estratégia reinicia a partir da direção inicial.
- **Parâmetros**
  - `InitialVolume` – volume da primeira ordem (padrão 1).
  - `LotExponent` – multiplicador para tamanhos de ordens subsequentes (padrão 1.6).
  - `PipStep` – distância em pontos entre os níveis da grade (padrão 30).
  - `TakeProfit` – alvo de lucro a partir do preço médio em pontos (padrão 10).
  - `MaxTrades` – número máximo de ordens ativas (padrão 10).
  - `StartLong` – abrir a primeira operação como comprado se true (padrão true).
  - `UseTrailingStop` – habilitar stop trailing (padrão false).
  - `TrailStart` – lucro em pontos para iniciar o trailing (padrão 10).
  - `TrailStop` – distância do trailing em pontos (padrão 10).
  - `CandleType` – período dos candles (padrão 1 minuto).
- **Filtros**
  - Categoria: Grade
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Opcional
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
