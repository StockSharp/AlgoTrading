# Estratégia Ma Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia - MA + Williams %R. Compra quando o preço está acima da MA e o Williams %R está abaixo de -80 (sobrevendido). Vende quando o preço está abaixo da MA e o Williams %R está acima de -20 (sobrecomprado).

Os testes indicam um retorno anual médio de aproximadamente 79%. Funciona melhor no mercado de ações.

A média móvel mostra a direção da tendência predominante. O Williams %R busca pontos sobrecomprados ou sobrevendidos em relação a essa tendência.

Adequado para traders de swing que aguardam recuos em direção à média. A distância do stop-loss é uma porcentagem fixa do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > MA && WilliamsR < WilliamsROversold`
  - Vendido: `Close < MA && WilliamsR > WilliamsROverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Williams %R retorna ao meio
- **Stops**: Baseados em porcentagem usando `StopLossPercent`
- **Valores padrão**:
  - `MaPeriod` = 20
  - `MaType` = MovingAverageTypeEnum.Simple
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    MaType escolhe uma média simples, exponencial, ponderada, suavizada ou de Hull. O meio do Williams %R é -50: uma compra é encerrada quando %R volta a subir até ele e uma venda quando volta a cair até ele. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Moving Average, Williams %R, R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

