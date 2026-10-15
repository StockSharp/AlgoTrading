# Estratégia EMA Moving Away
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

EMA Moving Away rastreia o quanto o preço se afasta de uma média móvel exponencial.
Quando uma sequência de velas empurra o preço um percentual definido abaixo do EMA,
a estratégia aposta em um retorno à média.

O setup foca no lado comprado: após um movimento baixista prolongado que leva o preço
abaixo do EMA em `MovingAwayPercent`, uma posição é aberta. Filtros de tamanho do
corpo e sequência podem ser adicionados para garantir que o movimento esteja esticado
em vez de ruidoso. Um stop-loss percentual protege o capital caso a reversão falhe.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Fechamento abaixo do EMA em `MovingAwayPercent` com filtros de sequência/tamanho requeridos.
  - **Vendido**: não utilizado.
- **Critérios de saída**: Retorno ao EMA ou acionamento do stop-loss.
- **Stops**: Stop percentual baseado em `StopLossPercent`.
- **Valores padrão**:
  - `EmaLength` = 55
  - `MovingAwayPercent` = 2.0
  - `StopLossPercent` = 2.0
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado
  - Indicadores: EMA
  - Complexidade: Moderado
  - Nível de risco: Médio
