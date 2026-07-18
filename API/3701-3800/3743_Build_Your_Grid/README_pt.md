# Construa sua estratégia de rede
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Build Your Grid** é uma conversão direta do consultor especialista MetaTrader "BuildYourGridEA". Mantém dois independentes
dente escadas de posições de mercado no lado longo e curto, adiciona novas camadas quando o preço avança por um número configurável de pip
se opcionalmente aumenta o volume negociado geométrica ou exponencialmente. A cesta pode ser fechada quando uma meta de lucro combinada
et é alcançado, quando uma perda máxima medida em pips é excedida, ou através da emissão de ordens de hedge sempre que o breac de drawdown flutuante
ele é uma porcentagem do saldo da conta.

## Como funciona

1. **Entradas iniciais.** Dependendo da *Colocação da Ordem*, a estratégia abre a primeira ordem de compra, venda ou ambas as ordens de mercado assim que a condição de spread permitir.
2. **Expansão da grade.** Pedidos adicionais são acionados com a tendência ou contra ela. A distância até a próxima camada é medida em pips, opcionalmente multiplicada pelo número de pedidos já abertos ou por uma potência de dois.
3. **Progressão de volume.** O tamanho do pedido segue a regra de progressão do lote selecionado (estático, geométrico ou exponencial) e pode ser limitado pelo *Multiplicador máximo* em relação à primeira entrada.
4. **Realização de lucros.** A cesta inteira é fechada quando o PnL flutuante agregado excede a meta expressa em pips ou na moeda da conta.
5. **Proteção contra perdas.** Quando a perda cumulativa ultrapassa o limite de pip configurado, a estratégia fecha o ticket mais antigo de cada lado ou a cesta inteira, dependendo do modo *Tratamento de perdas*.
6. **Hedging.** Se o rebaixamento flutuante atingir o *Limiar de Hedge (%)*, uma ordem de equilíbrio dimensionada pela diferença de volume e pelo *Multiplicador de Hedge* é submetida à exposição congelada.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `Order Placement` | Quais direções são permitidas para abrir novas camadas (ambas, apenas longas, apenas curtas). |
| `Grid Direction` | Se os pedidos adicionais seguem a tendência ou diminuem o movimento. |
| `Grid Step (pips)` | Distância base em pips até a próxima camada antes que os multiplicadores sejam aplicados. |
| `Step Progression` | Distância estática, crescimento geométrico (× contagem) ou crescimento exponencial (× 2^(n-1)). |
| `Close Target` | Tipo de meta de lucro (pips ou moeda da conta). |
| `Target (pips)` / `Target (currency)` | Limite que deve ser ultrapassado para fechar a cesta com lucro. |
| `Loss Handling` | Ação quando o limite de rebaixamento do pip for atingido (não fazer nada, fechar os primeiros tickets ou fechar todos). |
| `Loss (pips)` | Perda combinada máxima tolerada antes que a proteção seja acionada. |
| `Use Hedge` | Permite que ordens de hedge equilibrem a exposição líquida durante rebaixamentos profundos. |
| `Hedge Threshold (%)` | Percentual do saldo da conta utilizado como gatilho para hedge. |
| `Hedge Multiplier` | Multiplicador aplicado à diferença de volume na emissão da ordem de hedge. |
| `Auto Volume` / `Risk Factor` | Dimensionamento da posição acionada pelo equilíbrio. Volume = Saldo × Fator de Risco / 100.000. |
| `Manual Volume` | Tamanho do lote corrigido quando o dimensionamento automático está desativado. |
| `Lot Progression` | Escala estática, geométrica ou exponencial para pedidos consecutivos. |
| `Max Multiplier` | Limita o tamanho do lote para `firstLot × MaxMultiplier`. |
| `Max Orders` | Número máximo de posições abertas simultâneas (0 = ilimitado). |
| `Max Spread` | Bloqueia novas negociações enquanto o spread em pips estiver acima do limite (0 = ignorar). |
| `Use Completed Bar` / `Candle Type` | Avalie os sinais apenas uma vez por vela concluída do tipo selecionado. |

## Notas de uso

- A estratégia depende das melhores atualizações de compra/venda. Configure seu feed de dados para fornecer cotações de nível 1 com spreads precisos.
- As ordens de hedge dependem do valor do portfólio. Ao executar no StockSharp Designer ou Tester, certifique-se de que o portfólio conectado relate um equilíbrio significativo.
- As estratégias de grade acumulam riscos rapidamente. Comece com volumes conservadores e teste a configuração em simulação antes de aplicá-la à negociação ao vivo.
- Quando `Use Completed Bar` está ativado, a lógica de negociação é avaliada apenas uma vez por vela concluída, o que imita a opção "Usar barra concluída" do consultor original.
