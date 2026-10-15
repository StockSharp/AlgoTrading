# Estratégia EMA Sticker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza uma Média Móvel Exponencial (EMA) para seguir tendências de curto prazo. Uma posição comprada é aberta quando o preço de fechamento cruza acima da EMA, enquanto uma posição vendida é aberta quando cruza abaixo. Níveis opcionais fixos de stop-loss e take-profit ajudam a gerenciar o risco.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close > EMA`.
  - **Vendido**: `Close < EMA`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal oposto ou níveis de stop configurados atingidos.
- **Stops**: Sim, stop-loss e take-profit opcionais em unidades de preço.
- **Valores padrão**:
  - `MA period` = 5.
  - `Stop loss` = 0.001.
  - `Take profit` = 0.001.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Sim
  - Complexidade: Simples
  - Período: Curto prazo
