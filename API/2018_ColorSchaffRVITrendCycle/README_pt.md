# Estratégia de Ciclo de Tendência Color Schaff RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o Color Schaff RVI Trend Cycle usando a API de alto nível do StockSharp. O indicador aplica um processo de duplo estocástico à diferença entre os valores do Índice de Vigor Relativo rápido e lento e suaviza o resultado.

## Parâmetros
- `FastRviLength` – período para o cálculo do RVI rápido (padrão 23).
- `SlowRviLength` – período para o cálculo do RVI lento (padrão 50).
- `CycleLength` – comprimento dos ciclos estocásticos (padrão 10).
- `HighLevel` – limiar superior para detectar condições altistas (padrão 60).
- `LowLevel` – limiar inferior para detectar condições baixistas (padrão -60).
- `CandleType` – tipo de vela processado pela estratégia (período de 4 horas por padrão).

## Lógica de negociação
1. Calcular os valores RVI rápido e lento.
2. Construir o Schaff Trend Cycle a partir da diferença RVI.
3. **Comprar** quando o valor STC está acima do nível superior e subindo.
4. **Vender** quando o valor STC está abaixo do nível inferior e caindo.

## Notas
- A estratégia processa apenas velas finalizadas.
- A proteção de posição é ativada no início.
- Este exemplo é fornecido para fins educativos e não constitui conselho financeiro.
