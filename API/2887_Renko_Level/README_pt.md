# Estratégia de Nível Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Nível Renko é uma conversão fiel do consultor especializado MetaTrader 5 "Renko Level EA". Ela reconstrói a lógica orientada por indicadores dentro do StockSharp e negocia sempre que o nível Renko arredondado salta para um novo bloco. A estratégia interpreta cada mudança de nível como um rompimento do tijolo Renko sintético e entra na direção do rompimento ou na direção oposta quando o modo reverso está habilitado.

O sistema usa velas regulares baseadas em tempo (1 minuto por padrão) apenas como fonte de dados. Os fechamentos de velas são arredondados para um tamanho de bloco configurável que emula tijolos Renko sem a necessidade de subscrições de dados Renko. Cada vez que o bloco arredondado muda, a estratégia fecha qualquer exposição oposta e abre uma nova posição alinhada com o movimento detectado.

## Lógica de trading
1. **Inicialização**
   - Detectar o tamanho do pip do instrumento (`PriceStep`).
   - Converter o parâmetro `Block Size` de pips para unidades de preço (instrumentos de 3 e 5 dígitos multiplicam automaticamente o valor do pip por 10).
   - Arredondar o fechamento da primeira vela finalizada para o bloco mais próximo para criar os níveis Renko superiores e inferiores iniciais.
2. **Manutenção de níveis**
   - Em cada vela finalizada o preço de fechamento é arredondado para o tamanho de bloco mais próximo.
   - Quando o fechamento permanece dentro do bloco atual, os níveis armazenados permanecem inalterados.
   - Quando o fechamento rompe abaixo do limite inferior, o algoritmo arredonda o preço para baixo e desloca o bloco para baixo (`lower = round`, `upper = round + size`).
   - Quando o fechamento rompe acima do limite superior, o bloco é deslocado para cima (`upper = round`, `lower = round - size`).
3. **Geração de sinais**
   - Um nível superior crescente indica um rompimento altista do bloco Renko. Um nível superior decrescente indica um rompimento baixista.
   - Se `Reverse` estiver desabilitado, a estratégia compra em mudanças altistas e vende em mudanças baixistas. Quando `Reverse` está habilitado, as ações são trocadas.
   - Quando um sinal é acionado, a exposição existente na direção oposta é eliminada automaticamente (ordem de compra fecha vendidas, ordem de venda fecha compradas). Se `Allow Increase` estiver desabilitado, a estratégia recusa adicionar tamanho sobre uma posição já aberta na mesma direção.
4. **Execução de ordens**
   - As ordens são enviadas com a configuração `Volume` da estratégia. Ao reverter uma posição existente, o tamanho da ordem é igual à posição absoluta mais o volume configurado para que a inversão ocorra imediatamente.
   - `StartProtection()` é chamado durante a inicialização para que as proteções de risco configuradas no Designer ou via composição estejam ativas.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|---------|
| `Block Size` | Tamanho do bloco Renko em pips. A estratégia o multiplica pelo valor do pip do instrumento para obter o incremento de preço real. Valores maiores reduzem a frequência de trading. | 30 |
| `Reverse` | Quando `true`, inverte todos os sinais de trading (comprar em mudança baixista, vender em mudança altista). | `false` |
| `Allow Increase` | Quando `true`, permite a piramidação adicionando ordens adicionais na mesma direção em cada sinal. Quando `false`, uma nova ordem só é enviada se a posição líquida for plana após fechar o lado oposto. | `false` |
| `Candle Type` | Dados de velas fonte. Qualquer `DataType` suportado pode ser usado; por padrão a estratégia subscreve velas de 1 minuto. | `TimeFrame(1m)` |
| `Volume` *(herdado)* | Tamanho da ordem ao enviar ordens a mercado. Defina esta propriedade na instância da estratégia antes de iniciá-la. | Depende do portfólio |

## Notas de uso
- Escolha o tamanho do bloco de acordo com a volatilidade do instrumento. Para os principais pares de moedas, 30–50 pips emulam o comportamento do EA original. Em índices ou ativos cripto use tamanhos de bloco maiores.
- A estratégia funciona com qualquer fonte de velas (tick, período, range) desde que o fechamento da vela reflita a amostragem de preço desejada. Para uma fonte Renko pura, pode-se mudar o tipo de vela para uma série de dados Renko.
- Habilite `Reverse` para transformar o sistema de rompimento em um sistema de reversão à média que desvanece cada mudança de nível Renko.
- `Allow Increase` pode ser ativado para imitar o parâmetro "Increase" do EA original que adiciona contratos em cada novo nível na mesma direção.
- O risco e a gestão monetária (stop-loss, take-profit, controle de drawdown) podem ser configurados através de proteções do StockSharp ou estratégias envelope. O exemplo mantém a lógica idêntica ao especialista MT5 e não impõe saídas fixas além das mudanças de nível.

## Requisitos de dados
- Dados de velas históricos e em tempo real para o `Candle Type` configurado.
- Os metadados do instrumento devem fornecer `PriceStep` e `Decimals` para que a conversão de pip funcione corretamente. Quando esses valores não estão disponíveis, a estratégia recorre a um passo padrão de 0.0001.

## Fluxo de trabalho sugerido
1. Adicione a estratégia ao Designer ou crie-a programaticamente através da API do StockSharp.
2. Defina `Security`, `Portfolio`, `Volume` e, opcionalmente, ajuste os parâmetros listados acima.
3. Inicie a estratégia. Ela aguardará a primeira vela finalizada para estabelecer o bloco Renko inicial.
4. Monitore o gráfico de trades integrado ou subscreva aos logs para verificar que as ordens são acionadas apenas quando o nível arredondado muda.

Esta documentação reflete o comportamento do EA Renko Level original enquanto explica como está implementado dentro do StockSharp para que você possa personalizá-lo ou estendê-lo.
