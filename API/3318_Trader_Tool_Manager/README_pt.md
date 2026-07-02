# Painel manual TraderToolEA (port StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

O expert advisor original **TraderToolEA v1.8** do MetaTrader 4 não é um robô autônomo de negociação, mas um painel de controle que ajuda traders discricionários a gerenciar ordens, grids e níveis protetores. Este port recria o painel dentro do framework StockSharp. Em vez de botões no gráfico, a estratégia expõe parâmetros booleanos que se comportam como botões: defina-os como `true` na GUI ou em scripts para disparar a ação correspondente.

Principais capacidades traduzidas:

* Atalhos de ordens a mercado para abrir ou fechar exposição comprada/vendida.
* Colocação automática de grids simétricos feitos de ordens pendentes stop ou limit.
* Cancelamento seletivo de ordens pendentes (compra/venda/todas) com limpeza opcional de órfãs.
* Gestão virtual de stop-loss, take-profit, trailing stop e break-even movida por cotações Level1.
* Opção de dimensionamento automático que imita o cálculo de lote do MetaTrader (`AccountBalance / LotSize * RiskFactor`).

Toda a lógica depende exclusivamente da API de alto nível: assinaturas Level1, métodos auxiliares de ordens (`BuyStop`, `SellLimit`, `CancelOrder`...) e recursos integrados de logging.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `Use Auto Volume` | Quando `true`, a estratégia calcula o lote a partir do saldo da carteira e `Risk Factor`; caso contrário usa o `Order Volume` fixo. |
| `Risk Factor` | Multiplicador aplicado ao saldo da carteira durante o cálculo automático de volume. Equivale à entrada MT4 `RiskFactor`. |
| `Order Volume` | Lote manual usado para cada ordem a mercado ou pendente quando o auto sizing está desabilitado. |
| `Distance (pips)` | Espaçamento (em pips MetaTrader) entre ordens pendentes em camadas. Aplica-se a grids stop e limit. |
| `Layers` | Número de ordens pendentes adicionais por comando. `1` espelha um único clique no EA; valores maiores emulam vários cliques. |
| `Delete Orphans` | Quando habilitado, a estratégia cancela automaticamente ordens pendentes sem par para manter grids de compra/venda balanceados após execuções parciais. |
| `Enable Stop Loss` / `Stop Loss (pips)` | Ativa monitoramento de stop-loss fixo medido em pips relativo ao preço médio de entrada. |
| `Enable Take Profit` / `Take Profit (pips)` | Ativa monitoramento de take-profit fixo medido em pips. |
| `Enable Trailing` / `Trailing (pips)` | Habilita gestão virtual de trailing stop. O trail só arma quando o preço se move pelo menos `Trailing` pips a favor da posição. |
| `Enable Break-Even` / `Break-Even Trigger` / `Break-Even Lock` | Quando o preço avança pela distância de gatilho, o stop é movido para o preço de entrada mais o offset de trava (compras) ou menos o offset (vendas). |
| Chaves de comando (`Open Buy`, `Place Buy Stops`, `Delete Sell Limits`, ...) | Parâmetros booleanos que emulam os botões do EA. Defini-los como `true` executa a ação e a estratégia os redefine para `false`. |

## Fluxo de ordens

1. **Feed de dados:** a estratégia assina apenas `DataType.Level1`. Atualizações de melhor bid/ask conduzem a lógica de proteção e as colocações de grid.
2. **Normalização de volume:** antes de enviar qualquer ordem, o volume solicitado é arredondado para o `VolumeStep` do instrumento e limitado entre `MinVolume` e `MaxVolume`. Se os metadados estiverem ausentes, o valor bruto é usado.
3. **Ordens pendentes:** grids stop e limit são construídos ao redor do bid/ask mais recente. Preços são alinhados ao passo de preço do instrumento para evitar rejeições do motor de matching.
4. **Controle de órfãs:** quando `Delete Orphans` está habilitado, a estratégia mantém simétrico o número de ordens pendentes de compra e venda cancelando o lado excedente após execuções ou cancelamentos manuais. A mesma lógica é aplicada independentemente a grids stop e limit.
5. **Proteção virtual:** stop-loss, take-profit, trailing stop e break-even são implementados como guardas *virtuais*. Quando um limiar é violado, a estratégia envia uma ordem a mercado de fechamento para o volume restante e redefine o estado interno de trailing/break-even.

## Diferenças em relação ao MetaTrader

* Componentes gráficos (botões, caixas de texto, cores, sons) são substituídos por parâmetros StockSharp e logs. Cada ação escreve uma entrada informativa via `AddWarningLog` ou logger padrão.
* A lógica protetora opera em atualizações Level1 e fecha posições diretamente em vez de modificar preços de stop em ordens individuais. Isso mantém o comportamento consistente entre corretoras que não suportam stops no estilo MetaTrader.
* Os modos `ManageOrders` do MT4 (ID/manual/all/own) colapsam para o escopo da estratégia: apenas ordens criadas por esta estratégia são rastreadas e gerenciadas.
* O dimensionamento automático usa a avaliação da carteira em vez de `AccountBalance()`, mas fórmula e regras de arredondamento são preservadas.

## Dicas de uso

1. Configure os metadados do instrumento (`PriceStep`, `VolumeStep`, `MinVolume`, `LotSize`, ...) na conexão para que conversão de pips e arredondamento de volume correspondam às regras da corretora.
2. Vincule os parâmetros booleanos de comando a teclas de atalho ou botões de UI no terminal StockSharp para replicar a experiência original. As propriedades voltam para `false` após cada invocação bem-sucedida.
3. Habilite `Delete Orphans` ao trabalhar com grids simétricos para garantir que stops/limits remanescentes sejam limpos automaticamente quando um lado for acionado.
4. Monitore o log informativo: se a estratégia pular uma ação (por exemplo, porque bid/ask está indisponível ou o volume calculado é zero), um aviso é produzido com o motivo.
5. Como a proteção é virtual, mantenha a estratégia em execução enquanto posições estiverem abertas: ela fecha operações enviando ordens a mercado, não dependendo de stops no servidor.

## Notas de port

* O tamanho de pip espelha o MetaTrader: instrumentos com 3 ou 5 casas decimais multiplicam o passo de preço por 10 para transformar pontos em pips.
* Trailing stops e break-even seguem o fluxo do código MQL: armam apenas depois que o preço entra em lucro e usam variáveis de estado que reiniciam em novas operações, cancelamentos ou reversão de posição.
* O EA permitia pressionar botões várias vezes para estender grids. O parâmetro `Layers` emula isso criando vários níveis pendentes em uma chamada.
* Todos os controles manuais mantêm `SetCanOptimize(false)` para que campanhas de otimização não disparem ações acidentalmente.
