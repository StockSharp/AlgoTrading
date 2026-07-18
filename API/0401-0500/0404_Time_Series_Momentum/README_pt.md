# Estratégia de Momentum de Séries Temporais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta abordagem assume posições compradas ou vendidas em cada ativo com base em seus próprios retornos passados. Se o retorno acumulado for positivo, o modelo compra; se negativo, vende, formando uma carteira diversificada de seguidor de tendência.

Os sinais são avaliados mensalmente com períodos de retrospecto de um ano e as posições são igualmente ponderadas entre os ativos.

## Detalhes

- **Dados**: Retornos totais mensais de cada ativo.
- **Entrada**: Comprado quando o retorno de 12 meses > 0; vendido quando < 0.
- **Saída**: Reverter quando o sinal muda de sinal.
- **Instrumentos**: Amplo conjunto de futuros ou ETF.
- **Risco**: Escalonamento por volatilidade e diversificação.

