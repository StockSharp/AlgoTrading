# Estratégia de Breakout do Plano X
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de ruptura do Plano X replica o MetaTrader consultor especialista "plano x" de Peter Ingram. Ele se concentra na sessão de Londres no final da manhã e espera que o preço se afaste de uma vela de referência antes de entrar. Apenas uma posição líquida pode ser aberta por vez, e o risco é controlado por meio de stops baseados em pip que acompanham a negociação à medida que ela se move a favor.

## Lógica de negociação

1. **Âncora da sessão**
   - A estratégia observa velas de 15 minutos.
   - Na hora de início da sessão configurada (padrão 11h00), registra o fechamento daquela vela. Este fechamento atua como preço âncora para o resto da sessão.
   - A negociação só é considerada após o fechamento de pelo menos uma vela adicional e antes do horário de término da sessão (padrão 15:00).

2. **Condições de entrada**
   - **Longo**: Quando a última vela concluída fecha mais de `LongTargetPips` (padrão 25 pips) acima do fechamento da âncora e nenhuma posição está aberta.
   - **Short**: Quando a última vela concluída fecha mais de `ShortTargetPips` (padrão 20 pips) abaixo do fechamento da âncora e nenhuma posição está aberta.
   - Todas as comparações são feitas em unidades pip derivadas do tamanho do tick do instrumento.

3. **Gerenciamento de posição**
   - Um stop-loss inicial fixo igual a `InitialStopPips` (padrão 25 pips) é definido em relação ao preço de entrada.
   - O stop se converte em um trailing stop quando a negociação ganha pelo menos `TrailTriggerPips` (padrão 10 pips).
   - Cada vez que o preço avança mais `TrailTriggerPips`, o stop é movido `TrailStepPips` (padrão 5 pips) mais na direção lucrativa.
   - Se o preço atingir o stop, a posição será fechada no mercado.

4. **Volume**
   - Os pedidos usam o parâmetro `TradeVolume` (padrão 0,1 lote). Ajuste para corresponder ao tamanho do contrato de segurança.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `TradeVolume` | Volume de ordens de mercado utilizado para entradas e saídas. | 0,1 |
| `LongTargetPips` | Distância de fuga acima da âncora necessária para entradas longas. | 25 |
| `ShortTargetPips` | Distância de fuga abaixo da âncora necessária para entradas curtas. | 20 |
| `InitialStopPips` | Distância do preço de entrada ao stop loss de proteção. | 25 |
| `TrailTriggerPips` | Lucro em pips necessário antes que o trailing stop seja ativado ou avance. | 10 |
| `TrailStepPips` | Incremento de pip aplicado ao trailing stop cada vez que ele se move. | 5 |
| `SessionStartHour` | Hora decimal que indica quando a vela âncora começa (por exemplo, `11.0`, `11.5`). | 11,0 |
| `SessionEndHour` | Hora decimal após a qual nenhuma nova entrada é feita. Deve ser posterior a `SessionStartHour`. | 15,0 |
| `CandleType` | Série de velas usadas para avaliações. O padrão é velas de 15 minutos. | 15 minutos |

## Notas

- O tamanho do pip se adapta automaticamente com base no `PriceStep` do instrumento e na precisão decimal (3 ou 5 decimais recebem um multiplicador de 10x).
- A estratégia espera um mercado intradiário contínuo; em instrumentos com gaps diários, o comportamento de reancoragem ocorre a cada dia de negociação.
- Como as estratégias StockSharp usam posições líquidas, a conversão assume apenas uma direção aberta por vez. Isto reflete o comportamento padrão do especialista original quando nenhuma cobertura está ativa.

## Arquivos

- `CS/PlanXBreakoutStrategy.cs` – implementação em C# da lógica de breakout do Plano X para StockSharp.
