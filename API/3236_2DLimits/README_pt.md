# Estratégia de 2DLimits
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
2DLimits é uma portagem direta do expert advisor MetaTrader 4 `2DLimits_EA_v2`. A estratégia avalia as duas últimas velas diárias completadas e participa apenas quando formam um padrão em escada (máximas/mínimas mais altas ou máximas/mínimas mais baixas). Quando o padrão é válido, a estratégia submete ordens de stop na extremidade do dia anterior e protege a posição com um stop-loss no ponto médio e um alvo igual ao intervalo diário anterior.

A implementação depende das assinaturas de velas de alto nível do StockSharp junto com cotações de nível 1. As velas diárias fornecem os níveis de rompimento enquanto os snapshots do melhor bid/ask garantem que configurações compradas só sejam armadas quando o preço opera abaixo do ponto médio e configurações vendidas só quando o preço opera acima.

## Lógica da estratégia
### Filtro de estrutura diária
* A estratégia mantém uma janela deslizante de dois dias de velas diárias completadas (configurável pelo parâmetro de tipo de vela).
* Uma **configuração de alta** exige que o dia mais recente registre tanto uma máxima mais alta quanto uma mínima mais alta em comparação com o dia anterior.
* Uma **configuração de baixa** exige que o dia mais recente apresente tanto uma máxima mais baixa quanto uma mínima mais baixa que o dia anterior.
* O ponto médio do dia mais recente é calculado como `(high + low) / 2`, e o intervalo da vela é armazenado para o alvo de lucro.

### Regras de entrada
* Apenas um lote de ordens pendentes está ativo de cada vez; todas as ordens são canceladas e recalculadas quando uma nova vela diária fecha.
* Entradas compradas são preparadas quando:
  * O filtro de estrutura de alta está satisfeito.
  * O último preço ask está abaixo do ponto médio do dia anterior (espelha a verificação `Ask < middleY` do EA original).
  * Uma ordem de compra-stop é colocada exatamente na máxima do dia anterior.
* Entradas vendidas são preparadas quando:
  * O filtro de estrutura de baixa está satisfeito.
  * O último preço bid está acima do ponto médio do dia anterior (espelha `Bid > middleY`).
  * Uma ordem de venda-stop é colocada na mínima do dia anterior.
* Se ambas as verificações de estrutura falharem, nenhuma ordem fica ativa para a próxima sessão.

### Regras de saída
* Quando uma ordem de stop é acionada, a ordem de entrada oposta é cancelada imediatamente para que a estratégia nunca mantenha exposições compradas e vendidas simultaneamente.
* Após um rompimento comprado, duas ordens protetoras são registradas:
  * Uma ordem de stop no ponto médio do dia de referência atua como stop-loss.
  * Uma ordem de take-profit em `máxima anterior + intervalo anterior` corresponde à distância de take-profit do MetaTrader.
* Após um rompimento vendido, proteção simétrica é aplicada:
  * Uma ordem de stop no ponto médio (buy-stop) cobre o stop-loss.
  * Uma ordem de take-profit em `mínima anterior - intervalo anterior` espelha o alvo original.
* Ordens protetoras são reativadas sempre que o tamanho da posição executada muda e são removidas quando a posição retorna ao zero.

### Ciclo de vida da ordem e verificações de segurança
* Ordens pendentes são atualizadas apenas após a próxima vela diária completar, impondo uma única configuração por dia de negociação.
* A estratégia pula a geração de sinais sempre que já possui uma posição, evitando sobreposições entre configurações.
* O último snapshot de bid/ask é retido de `SubscribeLevel1()`; se não disponível, o último preço de transação é usado como fallback para evitar enviar ordens cegas.
* O arredondamento é realizado com o passo de preço do instrumento para que todas as ordens se alinhem com o tamanho de tick da bolsa.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Volume` | Volume da ordem para as entradas de stop. Deve ser maior que zero. |
| `CandleType` | Tipo de vela que fornece o intervalo de referência (padrão velas diárias). |

## Notas adicionais
* A estratégia gerencia cada ordem diretamente através da API de alto nível; não há dependência de coleções personalizadas ou buffers de indicadores.
* Apenas a implementação C# é fornecida neste pacote. Nenhuma versão Python é criada para esta conversão.
* Os testes permanecem inalterados conforme solicitado.
