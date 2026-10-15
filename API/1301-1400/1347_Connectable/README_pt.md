# Estratégia Conectável
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia modelo que pode ser conectada a fontes de sinais externas.
Suporta direções comprada e vendida e aplica stop-loss e take-profit baseados em porcentagem.

## Detalhes

- **Critérios de entrada**: sinal externo
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal externo ou stop-loss/take-profit
- **Stops**: Sim, baseado em porcentagem
- **Valores padrão**:
  - `CandleType` = 1 minuto
  - `StopLossPercent` = 2%
  - `TakeProfitPercent` = 4%
- **Filtros**:
  - Categoria: Outro
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
