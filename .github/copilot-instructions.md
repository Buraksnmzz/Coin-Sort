
---
applyTo: '**'
---
# Technical Instructions -- Unity Project Guidelines

## Onemli
- Gerekli şeyler serialized field yapacaksın. field'ların atamalarını ben yaparım.
- Mümkün olduğu kadar null kontrolü yapma. Runtime'da hatayı alınca düzeltirim ben gerekli şeyleri. 

## Kod Standartları

-   Tüm kod JetBrains Rider'ın default code style ve linter kurallarına
    uygun yazılacaktır.\
-   Kod içerisine yorum eklenmeyecek.\
-   Tüm sınıf, metod ve property isimleri açık, anlamlı ve
    self-explanatory olacaktır.

## Mimari & Prensipler

-   Tüm proje mimarisi **SOLID prensiplerine** uygun şekilde
    geliştirilecektir.\
-   Uygun görülen yerlerde `abstract` class, parent-child inheritance
    veya interface yapıları kullanılacaktır. Gereksiz yere de kullanılmayacaktır.\
-   Gerektiğinde **State, Strategy, Command, Observer** gibi tasarım
    kalıpları tercih edilecektir. Design Patternlar bunlarla sınırlı değildir, ihtiyaç duyulan durumlarda diğer kalıplar da kullanılabilir. Gereksiz yere de kullanılmayacaktır.

## UI Mimarisi

-   UI sistemi **MVC Pattern** ile geliştirilir.\
-   Her UI ekranı için:
    -   `BasePresenter`'dan türeyen bir **Presenter**,\
    -   `BaseView`'den türeyen bir **View** bulunacaktır.
-   UI açma--kapama işlemleri **UIService** üzerinden yapılacaktır.\
-   View sınıflarında inspector'dan atanması gereken tüm referanslar
    `[SerializeField] private` olarak bırakılacaktır.\
-   Presenter ve View arasında **event tabanlı iletişim** sağlanacaktır.

## Servisler ve Bağımlılıklar

-   Global bağımlılıklar **ServiceLocator** üzerinden erişilir.\
-   Modellerin yüklenmesi, saklanması ve kaydedilmesi için
    **SavedDataService** kullanılacaktır.\
-   Genel haberleşmeler için **EventDispatcherService** kullanılabilir.
    Bunun için gerekli **Signal** class'ları oluşturulur.\
-   Kodun her yerinde **decoupled** bir yapı tercih edilecektir.

## Kod Yazım Kuralları

-   Gereksiz null check yapılmayacak.\
-   **Magic number** gereksiz olarak kullanılmayacak; gerekli durumlarda enum veya constant tercih edilecektir.\
-   View dışı sınıflarda Unity API kullanımından mümkün olduğunca
    kaçınılacak.\
-   Data class'larında Unity bağımlılığı olmayacak (**POCO model**
    yaklaşımı).\
-   Bir sınıf tek bir işi yapacak (**Single Responsibility**).
-   ServiceLocator üzerinden alınan servisler ve SavedDataService üzerinden alınan modeller sistem gereği null kabul edilmez.
-   Bu tür referanslar için defensive null-check veya null-coalescing (??, ?.) kullanılmayacaktır.
-   Null ihtimali varsa bu durum mimari seviyede çözülmeli, kullanım seviyesinde değil.


## Nullability & Contract Kuralları
-   Hatalı null durumları fail-fast yaklaşımı ile runtime exception üretmelidir; sessizce tolere edilmeyecektir.
- Beklenmeyen null durumlarını asla sessizce tolere etme, gizleme veya etrafından dolanma. Hatanın anında tespit edilebilmesi için runtime'da patlamasına izin ver.
- Tüm `[SerializeField]` alanları, `GetComponent` referansları ve metot/constructor parametreleri mimari gereği kesinlikle null değildir. Bu alanlar için koruyucu null kontrolü yazma.
-   Mimari olarak null olması mümkün olmayan parametreler için if (x == null) kontrolü yapılmaz.
-   Defensive null-check yazılmayacaktır.
-   Parametre null gelebiliyorsa bu durum method içinde değil, çağıran katmanda çözülmelidir.
-   Fail-fast yaklaşımı tercih edilir; beklenmeyen null durumları runtime exception ile görünür olmalıdır.
-   Kod hata gizlemeyecek, hata görünür olacaktır.

## Özel Kurallar

-   **YoogoLabManager** class'ına kesinlikle dokunulmayacak.\
-   Mevcut altyapıyı bozan veya bypass eden hızlı çözümler kabul
    edilmeyecek.\
-   Bağımlılık zincirinde **circular dependency** oluşturulmayacak.