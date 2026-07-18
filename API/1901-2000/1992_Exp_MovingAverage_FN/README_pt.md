# Estratégia Exp de Média Móvel FN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base em reversões da inclinação de uma média móvel exponencial (EMA). Entra comprado quando a EMA vira para cima após uma queda e entra vendido quando a EMA vira para baixo após uma alta. Os níveis opcionais de stop-loss e take-profit são definidos em unidades de preço absoluto.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A inclinação da EMA muda de descendente para ascendente.
  - **Vendido**: A inclinação da EMA muda de ascendente para descendente.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Reversão de inclinação oposta.
  - Ativação do stop-loss ou take-profit.
- **Stops**: Sim, usando distâncias de preço absoluto.
- **Valores padrão**:
  - `EMA Length` = 12
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
  - `Candle Type` = período de 4 horas
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único (EMA)
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
