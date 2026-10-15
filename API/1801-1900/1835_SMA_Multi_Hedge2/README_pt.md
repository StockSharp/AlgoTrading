# Estratégia SMA Multi Hedge2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia um instrumento base enquanto faz hedge com um instrumento correlacionado. A direção da tendência é determinada por uma Média Móvel Simples (SMA). Quando a correlação entre os instrumentos base e de hedge excede um limite, ambos os instrumentos são negociados para formar um par neutro ao mercado.

## Como funciona

1. Calcular a tendência do instrumento base usando uma SMA de comprimento configurável.
2. Medir a correlação entre os instrumentos base e de hedge usando a diferença entre o preço e sua própria SMA.
3. Se a correlação atingir o nível esperado, abrir posições em ambos os instrumentos. A direção do hedge pode seguir ou se opor à base dependendo da configuração.
4. As posições são fechadas automaticamente quando o lucro combinado atinge o valor alvo.

## Parâmetros

- `SmaPeriod` — período da SMA usado para detectar tendência. Padrão é 20.
- `CorrelationPeriod` — número de amostras usado para avaliar correlação. Padrão é 20.
- `ExpectedCorrelation` — correlação absoluta mínima necessária para ativar o hedge. Padrão é 0.8.
- `ProfitTarget` — meta de lucro total em unidades monetárias. Padrão é 30.
- `CandleType` — tipo de dados para assinatura de velas. Padrão é período de 1 minuto.
- `FollowBase` — se verdadeiro, o hedge opera na mesma direção quando a correlação é positiva.

## Indicadores

- SMA
- Correlação (cálculo personalizado)

## Notas

Esta é uma portagem simplificada da estratégia MQL original. O gerenciamento de risco e dinheiro deve ser ajustado antes da negociação ao vivo.

