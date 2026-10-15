# Estratégia Virtual Profit/Loss Trail
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`VirtualProfitLossTrailStrategy` reproduz no StockSharp o comportamento do expert advisor do MetaTrader "Virtual Profit Loss Trail". A estratégia nunca abre novas posições por conta própria. Em vez disso, supervisiona continuamente a posição atual do ativo selecionado e aplica lógica de proteção:

- Uma distância configurável de take-profit expressa em pips.
- Uma distância configurável de stop-loss expressa em pips.
- Um trailing stop virtual que ativa após um lucro mínimo ser atingido e desliza com o mercado apenas quando o preço avança pelo passo de trailing especificado.

Como os níveis de proteção são virtuais, nenhuma ordem stop ou limit real é enviada à bolsa. A estratégia monitora atualizações de melhor bid/ask e fecha a posição aberta com uma ordem a mercado quando qualquer nível virtual é tocado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| **Take-profit (pips)** | Distância entre o preço de entrada e o alvo de lucro. Defina como `0` para desabilitar a saída por take-profit. |
| **Stop-loss (pips)** | Distância entre o preço de entrada e o stop de proteção. Defina como `0` para desabilitar a saída por stop-loss. |
| **Trailing stop (pips)** | Distância usada para calcular o trailing stop. Quando definida como `0`, a lógica trailing é totalmente desabilitada. |
| **Trailing step (pips)** | Lucro adicional que deve ser obtido antes de deslocar ainda mais o trailing stop. Use `0` para mover o trail sempre que uma nova máxima/mínima for impressa. |
| **Trailing activation (pips)** | Lucro mínimo que deve ser travado antes de o trailing stop ficar ativo. Quando definido como `0`, trailing começa imediatamente após entrar na posição. |

Todas as distâncias são medidas em unidades de pip. A estratégia deriva automaticamente o tamanho de pip a partir do passo de preço do ativo: para símbolos com três ou cinco casas decimais, um pip é definido como dez passos de preço; caso contrário, um passo.

## Lógica

1. **Assinatura de dados de mercado** - a estratégia assina dados Level1 para receber atualizações de melhor bid e melhor ask. Apenas atualizações finalizadas são processadas, garantindo que o algoritmo funcione em tempo real e durante replays históricos.
2. **Gestão de posição comprada** - quando a posição líquida é comprada, a estratégia calcula níveis virtuais de stop-loss, take-profit e trailing stop com base no preço médio de entrada. Se o melhor bid tocar o stop-loss ou take-profit, a posição é fechada imediatamente. Quando o lucro de ativação é alcançado, o trailing stop segue o preço para cima. O stop só avança quando o requisito de passo trailing é satisfeito.
3. **Gestão de posição vendida** - a mesma lógica é aplicada simetricamente usando o melhor ask para saídas de posições vendidas.
4. **Comportamento de reset** - sempre que a posição é totalmente fechada, referências internas de trailing são redefinidas para evitar sinais acidentais de reentrada.

## Dicas de uso

- Anexe a estratégia a um conector e ativo que já tenha uma posição aberta ou receberá ordens de outras estratégias ou negociação manual. O gestor controlará o tamanho agregado da posição.
- Garanta que dados Level1 estejam disponíveis; sem valores atuais bid/ask, os níveis virtuais não podem ser avaliados.
- A estratégia pode ser combinada com qualquer estratégia geradora de entradas executando ambas sob o mesmo portfólio e ativo. Apenas uma instância deve gerenciar a lógica de proteção para evitar conflitos.

## Diferenças em relação ao expert MQL

- A versão StockSharp trabalha com posições agregadas em vez de tickets de ordens individuais. Ela calcula automaticamente o preço médio de entrada fornecido pela plataforma.
- Desenho visual de linhas e alertas sonoros do expert original são substituídos por logging dentro do StockSharp. Ações de proteção são visíveis no diário da estratégia.
- A mesma configuração baseada em pips é preservada, incluindo o limite de ativação trailing e o passo incremental de trailing.

## Arquivos

- `CS/VirtualProfitLossTrailStrategy.cs` - implementação C# da estratégia.
- `README.md` - esta documentação.
- `README_zh.md` - tradução para chinês simplificado.
- `README_ru.md` - tradução para russo.
