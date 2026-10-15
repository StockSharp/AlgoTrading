# Estratégia de Média de Potência Bulls & Bears
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Port do especialista MetaTrader 5 `MySystem.mq5` localizado em `MQL/22016`.
- Detecta reversões de momentum calculando a média dos valores de Elder Bulls Power e Bears Power calculados a partir dos extremos das velas e uma EMA.
- Entra **comprado** quando a média aumenta enquanto ainda está abaixo de zero (a pressão baixista está diminuindo) e **vendido** quando a média diminui enquanto ainda está acima de zero (a pressão altista está diminuindo).
- Projetado para uma posição aberta de cada vez; stop-loss e take-profit são opcionais e expressos em pips.

## Lógica do indicador
| Componente | Descrição |
|-----------|-------------|
| Exponential Moving Average (EMA) | Aplicado aos preços de fechamento das velas. O parâmetro `MaPeriod` controla a janela de suavização (padrão 5). |
| Bulls Power (derivado) | Calculado como `High - EMA`. Mede a força altista relativa à EMA. |
| Bears Power (derivado) | Calculado como `Low - EMA`. Mede a força baixista relativa à EMA. |
| Potência média | `(Bulls Power + Bears Power) / 2`. Este oscilador sintético é comparado com seu valor anterior para detectar mudanças de momentum. |

A estratégia avalia as últimas duas velas finalizadas. Novos negócios são avaliados apenas quando uma vela está completamente completada para evitar ruído intrabar.

## Regras de entrada
1. Aguardar a EMA estar completamente formada (isto é, processou pelo menos `MaPeriod` velas).
2. Calcular Bulls Power e Bears Power para a vela recém-fechada usando seu high/low e o valor da EMA.
3. Calcular a média de ambas as forças para obter a leitura atual do oscilador.
4. Comparar com a média anterior:
   - **Configuração comprada**: média anterior `<` média atual **e** média atual `< 0`. Entrar comprado se não houver posição existente.
   - **Configuração vendida**: média anterior `>` média atual **e** média atual `> 0`. Entrar vendido se plano.
5. Uma vez em um negócio, depender de ordens de proteção opcionais ou gestão manual. O algoritmo não gera sinais de saída além do stop-loss/take-profit.

## Gestão de risco
- `StopLossPips`: Distância de stop absoluta opcional em pips (0 desabilita o stop). Convertido usando o `PriceStep` do instrumento.
- `TakeProfitPips`: Objetivo de lucro absoluto opcional em pips (0 desabilita o objetivo).
- Ordens de proteção são registradas assim que a posição é aberta via `StartProtection` com execução de mercado.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `OrderVolume` | 0.1 | Tamanho de ordem para entradas de mercado. |
| `StopLossPips` | 15 | Distância de stop-loss em pips. Definir como `0` para desabilitar. |
| `TakeProfitPips` | 95 | Distância de take-profit em pips. Definir como `0` para desabilitar. |
| `MaPeriod` | 5 | Comprimento da EMA usado para o cálculo de Bulls/Bears Power. |
| `CandleType` | Período de 1 hora | Série de velas usada para todos os cálculos (alterar para corresponder ao seu feed de dados). |

## Notas de uso
1. Vincular a estratégia a um instrumento e garantir que `CandleType` corresponda ao período pretendido.
2. Ajustar `OrderVolume`, `StopLossPips` e `TakeProfitPips` para atender aos requisitos do corretor.
3. Executar a estratégia; ela se inscreve automaticamente em velas, atualiza a EMA e emite ordens de mercado em sinais qualificados.
4. Apenas uma posição pode existir de cada vez. Quando um negócio está ativo, novos sinais são ignorados até que as ordens de proteção fechem a posição ou ela seja fechada manualmente.
5. Como a versão MQL original usou `InpBarCurrent = 1`, este port sempre trabalha em velas completamente fechadas e compara valores consecutivos; nenhum recálculo intrabar é realizado.

## Diferenças vs. Especialista MQL Original
- Usa a API `Strategy` de alto nível do StockSharp com inscrições de velas e vinculação de indicadores em vez de acesso manual a buffers.
- Deriva automaticamente os pips de `PriceStep` em vez de ajustes manuais de dígitos.
- Ignora o gerenciamento de ordens comentado original e depende da proteção de posição incorporada.
- Mantém a restrição de posição única da fonte ignorando sinais enquanto uma posição existe.

## Recomendações de teste
- Backtest no símbolo/período pretendido com dados históricos que incluam preços high/low para cálculo preciso de Bulls/Bears.
- Validar o comportamento das ordens de proteção com o tamanho do tick e o passo de volume do seu corretor antes de executar ao vivo.
- Experimentar com diferentes valores de `MaPeriod` para adaptar a sensibilidade à volatilidade do instrumento.
