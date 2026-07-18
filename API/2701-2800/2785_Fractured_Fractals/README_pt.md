# Estratégia Fractured Fractals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Port do clássico expert advisor MetaTrader "Fractured Fractals". A estratégia rastreia fractais de Williams confirmados, coloca ordens stop em novos níveis de rompimento e segue um stop de proteção no fractal oposto.

## Detalhes

- **Fonte**: Convertido de `MQL/20127/Fractured Fractals.mq5`.
- **Regime de mercado**: Continuação de rompimento em qualquer instrumento suportado pelo StockSharp.
- **Tipos de ordens**: Usa ordens stop para entradas e ordens stop de proteção para saídas.
- **Dimensionamento de posição**: Baseado em risco, controlado por `MaximumRiskPercent` e a lógica de sequência adaptativa `DecreaseFactor`.
- **Parâmetros padrão**:
  - `MaximumRiskPercent` = 2%
  - `DecreaseFactor` = 10
  - `ExpirationHours` = 1 hora
  - `CandleType` = Período de 1 hora
- **Indicadores principais**: Fractais nativos de Williams de cinco barras calculados em tempo real.
- **Tipo de estratégia**: Rompimento comprado/vendido com gestão dinâmica de stops.

## Lógica da estratégia

### Rastreamento da sequência de fractais

- Mantém filas dos últimos cinco máximos e mínimos de velas para imitar o buffer `iFractals` no MT5.
- Cada fractal confirmado desloca três slots rotacionais: mais jovem, médio e antigo. Valores duplicados são ignorados usando o passo de preço do instrumento como tolerância.
- Os sinais comprados requerem que o fractal de alta mais recente supere o fractal médio; os sinais vendidos requerem que o fractal de baixa mais recente seja inferior ao anterior.

### Ordens de entrada e expiração

- Quando não existe posição comprada ou ordem de compra stop pendente, a estratégia coloca um buy stop no fractal de alta mais recente com um stop loss no fractal de baixa mais recente.
- Simetricamente, as entradas vendidas colocam um sell stop no fractal de baixa mais recente com um stop de proteção no fractal de alta mais recente.
- As ordens pendentes herdam uma expiração definida por `ExpirationHours`. Se o tempo da vela ultrapassar a expiração ou a hierarquia de fractais invalidar a configuração (novo fractal de alta mais baixo para comprados ou fractal de baixa mais alto para vendidos), a ordem é cancelada.
- O bot mantém o livro limpo cancelando ordens opostas assim que uma posição é aberta.

### Stops de proteção com trailing

- Cada fractal oposto confirmado atualiza a ordem de stop de proteção: posições compradas seguem o fractal de baixa mais recente, posições vendidas seguem o fractal de alta mais recente.
- Os stops são apenas ajustados — novos níveis devem melhorar sobre o preço da ordem existente antes de ocorrer uma substituição.
- Quando a posição é fechada, qualquer ordem de stop restante é cancelada imediatamente.

### Gestão de risco e controle de sequência

- `CalculateOrderVolume` replica o cálculo de risco do MT5: risco por unidade = preço de entrada menos preço de stop (ou vice-versa para vendidos).
- O risco monetário alvo equivale a `Portfolio.CurrentValue * MaximumRiskPercent / 100` com um fallback para a propriedade `Volume` quando a avaliação do portfólio não está disponível.
- O volume resultante é normalizado pelo tamanho do lote, passo de volume, volume mínimo e restrições de volume máximo expostas por `Security`.
- Após uma operação perdedora, o contador de sequência incrementa; operações lucrativas ou planas reiniciam o contador. Se ocorrer mais de uma perda consecutiva, o tamanho é reduzido por `losses / DecreaseFactor`.

### Rastreamento do resultado das operações

- `OnOwnTradeReceived` agrega execuções para determinar quando um ciclo de posição se completa e se terminou positivo, negativo ou plano.
- O contador de sequência e o último carimbo de tempo lucrativo espelham a lógica original, permitindo extensões adicionais (por ex., análises) se desejado.

## Notas de uso

1. Conecte a estratégia a qualquer par instrumento/portfólio, ajuste `CandleType` à resolução desejada e defina os parâmetros de risco de acordo com o tamanho da conta.
2. Certifique-se de que o adaptador/broker suporte ordens stop; caso contrário, substitua as ordens de proteção por saídas manuais em `UpdateTrailingStops`.
3. Como a implementação processa apenas velas concluídas, picos intra-barra menores que a resolução da vela não acionarão ordens exatamente como nos testes de MT5 baseados em ticks.
4. Considere habilitar o registro para revisar mensagens de comentários produzidas pelo port em C#, espelhando o feedback do expert original.
