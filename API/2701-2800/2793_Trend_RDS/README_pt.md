# Estratégia Trend RDS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Trend RDS busca sequências direcionais claras na ação do preço. Quando três velas completadas formam mínimas estritamente mais altas, trata a estrutura como um trecho de tendência de alta. Três máximas estritamente mais baixas marcam uma configuração de baixa. Uma regra de proteção bloqueia entradas quando as mesmas três barras criam simultaneamente tanto mínimas mais altas quanto máximas mais baixas, o que geralmente indica um triângulo em contração em vez de um movimento direcional. A estratégia pode opcionalmente inverter a direção através do parâmetro `Reverse`.

A negociação é limitada a uma janela de tempo configurável (padrão 09:00–12:00). Quando a janela está aberta e um padrão válido aparece, a estratégia fecha qualquer exposição oposta, abre uma nova posição a mercado no fechamento da vela e coloca ordens de stop-loss e take-profit medidas em pips. A distância em pips é derivada do passo de preço do instrumento, espelhando a lógica original do MetaTrader. Um trailing stop opcional move o stop de proteção para frente quando o preço avança pela distância de trailing mais o passo de trailing. Os ajustes de trailing são avaliados apenas enquanto a janela de sessão estiver ativa.

O tamanho da posição é recalculado em cada entrada. A estratégia aloca uma fração do capital do portfólio definida por `RiskPercent` e a divide pelo risco monetário representado pela distância de stop escolhida. Isso produz um dimensionamento dinâmico que escala com o tamanho da conta e a largura do stop, respeitando o valor mínimo `Volume`. Definir qualquer parâmetro relacionado ao risco como zero desabilita essa função, permitindo entradas de tamanho fixo ou sem proteção quando desejado.

## Detalhes
- **Critérios de entrada**: Três velas consecutivas com mínimas mais altas ativam compras (ou vendas quando `Reverse` é verdadeiro). Três mínimas consecutivas mais baixas ativam vendas (ou compras no modo reverso). Os sinais são ignorados se as mesmas três barras também satisfizerem ambas as condições simultaneamente.
- **Comprado/Vendido**: Ambas as direções com um interruptor de reversão opcional.
- **Critérios de saída**: Saídas a mercado quando os níveis de stop-loss, take-profit ou trailing stop rastreados são violados.
- **Stops**: Stop-loss e take-profit fixos em pips com trailing stop incremental (requer que ambos os parâmetros de trailing sejam positivos).
- **Janela de tempo**: Opera apenas entre `StartTime` e `EndTime` (padrão 09:00–12:00 horário da bolsa).
- **Dimensionamento de posição**: Dimensionamento baseado em risco usando `RiskPercent` do capital do portfólio relativo à distância de stop atual (recorre a `Volume` se o dimensionamento não puder ser calculado).
- **Valores padrão**:
  - `StopLossPips` = 30
  - `TakeProfitPips` = 65
  - `TrailingStopPips` = 0
  - `TrailingStepPips` = 5
  - `RiskPercent` = 3
  - `StartTime` = 09:00
  - `EndTime` = 12:00
  - `Reverse` = false
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Ação do preço (máximas/mínimas)
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
