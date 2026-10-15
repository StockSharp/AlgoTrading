# Estratégia de Pullback Lucrativo Mark804
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia de pullback seguidora de tendência que utiliza uma faixa de médias móveis exponenciais. O sistema procura retrações do preço em direção à EMA de sinal dentro de uma tendência confirmada. Quando o preço fecha novamente na direção da tendência após um pullback, a estratégia abre uma posição e a protege com níveis de take profit e stop loss baseados em porcentagem.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida > EMA sinal > EMA média, opcionalmente EMA média > EMA lenta, fechamento anterior abaixo da EMA sinal e fechamento atual acima.
  - **Vendido**: EMA rápida < EMA sinal < EMA média, opcionalmente EMA média < EMA lenta, fechamento anterior acima da EMA sinal e fechamento atual abaixo.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Take profit ou stop loss atingido.
- **Stops**: Sim, porcentagens fixas de take profit e stop loss.
- **Valores padrão**:
  - Fast EMA Length = 8
  - Signal EMA Length = 21
  - Medium EMA Length = 50
  - Slow EMA Length = 200
  - Take Profit % = 2
  - Stop Loss % = 1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
