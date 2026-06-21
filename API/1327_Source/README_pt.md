# Estratégia Source
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Source entra comprado quando o candle fecha acima da abertura e vendido quando fecha abaixo. Percentuais opcionais de stop loss, take profit e trailing stop gerenciam a posição aberta.

## Detalhes

- **Critérios de entrada**: comprado quando fechamento > abertura, vendido quando fechamento < abertura
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto ou ativação do gerenciamento de stops
- **Stops**: Stop loss, take profit e trailing stop opcionais
- **Valores padrão**:
  - `SL %` = 1
  - `TP %` = 3
  - `Trail Points %` = 3
  - `Trail Offset %` = 1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
